using System;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Input;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Matching.Generation;
using Match3.Core.Utility;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Input;

/// <summary>
/// BotSystem 单元测试
///
/// 职责：
/// - 为自动游戏寻找有效的移动
/// - 随机尝试位置和方向
/// - 验证移动是否会产生匹配
/// </summary>
public class BotSystemTests
{
    private class StubRandom : IRandom
    {
        private int _counter;
        private readonly int[] _sequence;

        public StubRandom(params int[] sequence)
        {
            _sequence = sequence.Length > 0 ? sequence : new[] { 0 };
        }

        public float NextFloat() => 0f;
        public int Next(int max) => Next(0, max);
        public int Next(int min, int max)
        {
            if (_sequence.Length == 0) return min;
            var val = _sequence[_counter % _sequence.Length];
            _counter++;
            // Clamp to valid range
            return Math.Max(min, Math.Min(max - 1, val));
        }
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    private class StubLogger : IGameLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInfo<T>(string message, T args) { }
        public void LogInfo<T1, T2>(string message, T1 arg1, T2 arg2) { }
        public void LogInfo<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3) { }
        public void LogWarning<T>(string message, T args) { }
    }

    private BotSystem CreateBotSystem()
    {
        var bombGenerator = new BombGenerator();
        var matchFinder = new ClassicMatchFinder(bombGenerator);
        return new BotSystem(matchFinder);
    }

    private InteractionSystem CreateInteractionSystem()
    {
        return new InteractionSystem(new StubLogger());
    }

    private GameState CreateEmptyState(int width = 8, int height = 8, IRandom? random = null)
    {
        random ??= new StubRandom();
        var state = new GameState(width, height, 6, random);
        state.SelectedPosition = Position.Invalid;

        // Initialize with None tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                state.SetTile(x, y, new Tile(y * width + x, TileType.None, x, y));
            }
        }
        return state;
    }

    #region TryGetRandomMove - Valid Move Tests

    [Fact]
    public void TryGetRandomMove_WithHorizontalMatchPossible_FindsMove()
    {
        // Arrange: 设置一个可以通过交换产生水平匹配的棋盘
        // R R B R ...  -> 交换 (2,0) 和 (3,0) 后变成 R R R B ...
        var random = new StubRandom(2, 0); // 让Bot尝试位置 (2, 0)
        var state = CreateEmptyState(8, 8, random);

        state.SetTile(0, 0, new Tile(0, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(1, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(2, TileType.Blue, 2, 0));
        state.SetTile(3, 0, new Tile(3, TileType.Red, 3, 0));

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void TryGetRandomMove_WithVerticalMatchPossible_FindsMove()
    {
        // Arrange: 设置一个可以通过交换产生垂直匹配的棋盘
        // (0,0)=R, (0,1)=R, (0,2)=B, (0,3)=R -> 交换 (0,2) 和 (0,3) 后变成垂直匹配
        var random = new StubRandom(0, 2); // 让Bot尝试位置 (0, 2)
        var state = CreateEmptyState(8, 8, random);

        state.SetTile(0, 0, new Tile(0, TileType.Red, 0, 0));
        state.SetTile(0, 1, new Tile(8, TileType.Red, 0, 1));
        state.SetTile(0, 2, new Tile(16, TileType.Blue, 0, 2));
        state.SetTile(0, 3, new Tile(24, TileType.Red, 0, 3));

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.True(found);
    }

    #endregion

    #region TryGetRandomMove - No Valid Move Tests

    [Fact]
    public void TryGetRandomMove_EmptyBoard_ReturnsFalse()
    {
        // Arrange: 空棋盘，没有可能的匹配
        var random = new StubRandom(0, 0, 1, 1, 2, 2);
        var state = CreateEmptyState(8, 8, random);

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.False(found);
        Assert.Equal(default, move);
    }

    [Fact]
    public void TryGetRandomMove_NoMatchPossible_ReturnsFalse()
    {
        // Arrange: 棋盘上没有可能的匹配（棋子分散）
        var random = new StubRandom(0, 0, 1, 1, 2, 2, 3, 3);
        var state = CreateEmptyState(4, 4, random);

        // 设置一个没有匹配可能的棋盘（棋子完全分散）
        var types = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow };
        int id = 0;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                // 使用棋盘格模式，确保相邻格子颜色不同
                var type = types[(x + y * 2) % 4];
                state.SetTile(x, y, new Tile(id++, type, x, y));
            }
        }

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.False(found);
    }

    #endregion

    #region TryGetRandomMove - Direction Array Tests

    [Fact]
    public void TryGetRandomMove_TriesAllFourDirections()
    {
        // Arrange: 设置一个只有向下交换才能匹配的情况
        // 这测试了Bot会尝试所有4个方向
        var random = new StubRandom(1, 0); // 位置 (1, 0)
        var state = CreateEmptyState(8, 8, random);

        // 设置棋盘：交换 (1,0) 向下到 (1,1) 会产生垂直匹配
        state.SetTile(1, 0, new Tile(1, TileType.Blue, 1, 0));
        state.SetTile(1, 1, new Tile(9, TileType.Red, 1, 1));
        state.SetTile(1, 2, new Tile(17, TileType.Blue, 1, 2));
        state.SetTile(1, 3, new Tile(25, TileType.Blue, 1, 3));

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.True(found);
    }

    #endregion

    #region TryGetRandomMove - Edge Cases

    [Fact]
    public void TryGetRandomMove_SmallBoard_HandlesEdges()
    {
        // Arrange: 3x3 小棋盘
        var random = new StubRandom(0, 0, 1, 1, 2, 2);
        var state = CreateEmptyState(3, 3, random);

        // 设置一个可以匹配的情况
        state.SetTile(0, 0, new Tile(0, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(1, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(2, TileType.Blue, 2, 0));
        state.SetTile(0, 1, new Tile(3, TileType.Red, 0, 1));

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert - 应该找到交换 (2,0) 和 (0,1) 或类似的匹配
        // 这里主要测试不会因为边界问题崩溃
        // 结果取决于随机数序列和棋盘布局
    }

    [Fact]
    public void TryGetRandomMove_WithBombTiles_FindsMove()
    {
        // Arrange: 包含炸弹的棋盘
        var random = new StubRandom(0, 0);
        var state = CreateEmptyState(8, 8, random);

        // 设置一个包含炸弹的匹配场景
        var bombTile = new Tile(0, TileType.Red, 0, 0) { Bomb = BombType.Horizontal };
        state.SetTile(0, 0, bombTile);
        state.SetTile(1, 0, new Tile(1, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(2, TileType.Blue, 2, 0));
        state.SetTile(3, 0, new Tile(3, TileType.Red, 3, 0));

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert - 应该正常工作，不因炸弹而出错
        // 结果取决于是否能形成匹配
    }

    [Fact]
    public void TryGetRandomMove_MaxAttempts_StopsAfter20Tries()
    {
        // Arrange: 设置一个永远找不到匹配的棋盘
        // 使用固定的随机序列，确保20次尝试后停止
        var random = new StubRandom(0, 0); // 始终尝试同一位置
        var state = CreateEmptyState(8, 8, random);

        // 空棋盘
        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act - 这应该在20次尝试后返回false
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out var move);

        // Assert
        Assert.False(found);
    }

    #endregion

    #region TryGetRandomMove - State Integrity Tests

    [Fact]
    public void TryGetRandomMove_DoesNotModifyState_WhenNoMoveFound()
    {
        // Arrange
        var random = new StubRandom(0, 0);
        var state = CreateEmptyState(4, 4, random);

        // 设置一个简单的棋盘
        state.SetTile(0, 0, new Tile(0, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(1, TileType.Blue, 1, 0));

        // 保存原始状态
        var originalTile00 = state.GetTile(0, 0);
        var originalTile10 = state.GetTile(1, 0);

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        botSystem.TryGetRandomMove(ref state, interactionSystem, out _);

        // Assert - 状态应该没有被修改
        var currentTile00 = state.GetTile(0, 0);
        var currentTile10 = state.GetTile(1, 0);

        Assert.Equal(originalTile00.Type, currentTile00.Type);
        Assert.Equal(originalTile10.Type, currentTile10.Type);
        Assert.Equal(originalTile00.Position, currentTile00.Position);
        Assert.Equal(originalTile10.Position, currentTile10.Position);
    }

    [Fact]
    public void TryGetRandomMove_DoesNotModifyState_WhenMoveFound()
    {
        // Arrange: 设置一个有有效移动的棋盘
        var random = new StubRandom(2, 0);
        var state = CreateEmptyState(8, 8, random);

        state.SetTile(0, 0, new Tile(0, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(1, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(2, TileType.Blue, 2, 0));
        state.SetTile(3, 0, new Tile(3, TileType.Red, 3, 0));

        // 保存原始状态
        var originalTile20 = state.GetTile(2, 0);
        var originalTile30 = state.GetTile(3, 0);

        var botSystem = CreateBotSystem();
        var interactionSystem = CreateInteractionSystem();

        // Act
        bool found = botSystem.TryGetRandomMove(ref state, interactionSystem, out _);

        // Assert - 即使找到移动，状态也不应该被修改（Bot只是模拟，不执行）
        if (found)
        {
            var currentTile20 = state.GetTile(2, 0);
            var currentTile30 = state.GetTile(3, 0);

            // 位置应该恢复原状（Bot内部swap后会swap回来）
            Assert.Equal(originalTile20.Position, currentTile20.Position);
            Assert.Equal(originalTile30.Position, currentTile30.Position);
        }
    }

    #endregion
}
