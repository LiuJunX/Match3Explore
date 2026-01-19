using System;
using System.Collections.Generic;
using Match3.Core.Events;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching;

/// <summary>
/// 棋盘洗牌系统实现
/// 使用 Fisher-Yates 算法重新洗牌普通色块，同时保留特殊棋子和 Cover 阻挡
/// </summary>
public sealed class BoardShuffleSystem : IBoardShuffleSystem
{
    private readonly IDeadlockDetectionSystem _deadlockDetector;

    /// <summary>
    /// 创建棋盘洗牌系统
    /// </summary>
    /// <param name="deadlockDetector">死锁检测系统，用于验证洗牌后有可行移动</param>
    public BoardShuffleSystem(IDeadlockDetectionSystem deadlockDetector)
    {
        _deadlockDetector = deadlockDetector ?? throw new ArgumentNullException(nameof(deadlockDetector));
    }

    /// <inheritdoc />
    public void Shuffle(ref GameState state, IEventCollector events)
    {
        var changes = ShuffleAndGetChanges(ref state);
        Pools.Release(changes); // 不需要保留，调用者如果需要会用 ShuffleUntilSolvable
    }

    /// <summary>
    /// 执行洗牌并返回变化列表
    /// </summary>
    private List<TileTypeChange> ShuffleAndGetChanges(ref GameState state)
    {
        var types = Pools.ObtainList<TileType>();
        var oldTypes = Pools.ObtainList<(Position Pos, TileType OldType, long TileId)>();
        var changes = Pools.ObtainList<TileTypeChange>();

        try
        {
            // 1. 收集阶段：收集所有可洗牌的普通色块类型及其位置
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    var pos = new Position(x, y);

                    // 跳过被 Cover 阻挡的位置
                    if (!state.CanMatch(pos))
                        continue;

                    var tile = state.GetTile(pos);

                    // 只收集普通色块（排除特殊棋子）
                    if (IsShuffleableTileType(tile.Type) && tile.Bomb == BombType.None)
                    {
                        types.Add(tile.Type);
                        oldTypes.Add((pos, tile.Type, tile.Id));
                    }
                }
            }

            // 2. 洗牌阶段：使用 Fisher-Yates 算法
            ShuffleTileTypes(types, state.Random);

            // 3. 分配阶段：重新分配洗牌后的类型到棋盘，并记录变化
            int index = 0;
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    var pos = new Position(x, y);

                    if (!state.CanMatch(pos))
                        continue;

                    var tile = state.GetTile(pos);

                    if (IsShuffleableTileType(tile.Type) && tile.Bomb == BombType.None)
                    {
                        var newType = types[index];
                        var oldInfo = oldTypes[index];
                        index++;

                        // 只记录实际改变的棋子
                        if (oldInfo.OldType != newType)
                        {
                            changes.Add(new TileTypeChange(tile.Id, pos, newType));
                        }

                        tile.Type = newType;
                        state.SetTile(pos, tile);
                    }
                }
            }

            return changes;
        }
        finally
        {
            Pools.Release(types);
            Pools.Release(oldTypes);
        }
    }

    /// <inheritdoc />
    public bool ShuffleUntilSolvable(ref GameState state, IEventCollector events, int maxAttempts = 10)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var changes = ShuffleAndGetChanges(ref state);

            try
            {
                // 发射洗牌事件（包含变化信息）
                if (events.IsEnabled)
                {
                    events.Emit(new BoardShuffledEvent
                    {
                        AttemptCount = attempt,
                        ScoreBefore = state.Score,
                        Changes = changes.ToArray()
                    });
                }

                // 检查是否有可行移动
                if (_deadlockDetector.HasValidMoves(in state))
                {
                    return true;
                }
            }
            finally
            {
                Pools.Release(changes);
            }
        }

        // 达到最大尝试次数仍无解
        return false;
    }

    /// <summary>
    /// 判断是否是可洗牌的普通色块类型（Red ~ Orange）
    /// </summary>
    private static bool IsShuffleableTileType(TileType type)
    {
        return type == TileType.Red
            || type == TileType.Green
            || type == TileType.Blue
            || type == TileType.Yellow
            || type == TileType.Purple
            || type == TileType.Orange;
    }

    /// <summary>
    /// 使用 Fisher-Yates 算法洗牌
    /// </summary>
    private static void ShuffleTileTypes(System.Collections.Generic.List<TileType> types, Random.IRandom random)
    {
        int n = types.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(0, n + 1);
            (types[k], types[n]) = (types[n], types[k]);
        }
    }
}
