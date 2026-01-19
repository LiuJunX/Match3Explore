using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Utility;

/// <summary>
/// 表示一个有效移动
/// </summary>
public readonly struct ValidMove
{
    public Position From { get; }
    public Position To { get; }

    public ValidMove(Position from, Position to)
    {
        From = from;
        To = to;
    }

    public void Deconstruct(out Position from, out Position to)
    {
        from = From;
        to = To;
    }
}

/// <summary>
/// 有效移动检测工具
/// 提供快速检测和枚举棋盘上所有可行移动的功能
/// </summary>
public static class ValidMoveDetector
{
    /// <summary>
    /// 快速检查是否存在至少一个有效移动（早期退出优化）
    /// </summary>
    /// <param name="state">游戏状态</param>
    /// <param name="matchFinder">匹配检测器</param>
    /// <returns>如果存在至少一个有效移动则返回 true，否则返回 false</returns>
    public static bool HasValidMoves(in GameState state, IMatchFinder matchFinder)
    {
        var stateCopy = state;

        // 检查所有水平相邻对
        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width - 1; x++)
            {
                var from = new Position(x, y);
                var to = new Position(x + 1, y);

                if (!GridUtility.IsSwapValid(in state, from, to))
                    continue;

                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);
                bool hasMatch = matchFinder.HasMatchAt(in stateCopy, from) ||
                               matchFinder.HasMatchAt(in stateCopy, to);
                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);

                if (hasMatch)
                    return true; // 早期退出
            }
        }

        // 检查所有垂直相邻对
        for (int y = 0; y < state.Height - 1; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                var from = new Position(x, y);
                var to = new Position(x, y + 1);

                if (!GridUtility.IsSwapValid(in state, from, to))
                    continue;

                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);
                bool hasMatch = matchFinder.HasMatchAt(in stateCopy, from) ||
                               matchFinder.HasMatchAt(in stateCopy, to);
                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);

                if (hasMatch)
                    return true; // 早期退出
            }
        }

        return false;
    }

    /// <summary>
    /// 查找所有有效移动（用于调试、测试、AI）
    /// </summary>
    /// <param name="state">游戏状态</param>
    /// <param name="matchFinder">匹配检测器</param>
    /// <returns>所有有效移动的列表，调用者负责使用 Pools.Release() 释放</returns>
    public static List<ValidMove> FindAllValidMoves(
        in GameState state, IMatchFinder matchFinder)
    {
        var validMoves = Match3.Core.Utility.Pools.Pools.ObtainList<ValidMove>();
        var stateCopy = state;

        // 水平交换
        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width - 1; x++)
            {
                var from = new Position(x, y);
                var to = new Position(x + 1, y);

                if (!GridUtility.IsSwapValid(in state, from, to))
                    continue;

                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);
                bool hasMatch = matchFinder.HasMatchAt(in stateCopy, from) ||
                               matchFinder.HasMatchAt(in stateCopy, to);
                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);

                if (hasMatch)
                {
                    validMoves.Add(new ValidMove(from, to));
                }
            }
        }

        // 垂直交换
        for (int y = 0; y < state.Height - 1; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                var from = new Position(x, y);
                var to = new Position(x, y + 1);

                if (!GridUtility.IsSwapValid(in state, from, to))
                    continue;

                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);
                bool hasMatch = matchFinder.HasMatchAt(in stateCopy, from) ||
                               matchFinder.HasMatchAt(in stateCopy, to);
                GridUtility.SwapTilesForCheck(ref stateCopy, from, to);

                if (hasMatch)
                {
                    validMoves.Add(new ValidMove(from, to));
                }
            }
        }

        return validMoves;
    }
}
