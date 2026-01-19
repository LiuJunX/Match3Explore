using System;
using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Core.Utility;

namespace Match3.Core.Systems.Matching;

/// <summary>
/// 死锁检测系统实现
/// 通过 ValidMoveDetector 工具类检测棋盘是否存在可行移动
/// </summary>
public sealed class DeadlockDetectionSystem : IDeadlockDetectionSystem
{
    private readonly IMatchFinder _matchFinder;

    /// <summary>
    /// 创建死锁检测系统
    /// </summary>
    /// <param name="matchFinder">匹配检测器</param>
    public DeadlockDetectionSystem(IMatchFinder matchFinder)
    {
        _matchFinder = matchFinder ?? throw new ArgumentNullException(nameof(matchFinder));
    }

    /// <inheritdoc />
    public bool HasValidMoves(in GameState state)
        => ValidMoveDetector.HasValidMoves(in state, _matchFinder);

    /// <inheritdoc />
    public List<ValidMove> FindAllValidMoves(in GameState state)
        => ValidMoveDetector.FindAllValidMoves(in state, _matchFinder);

    /// <inheritdoc />
    public void InvalidateCache()
    {
        // 预留接口，当前实现无缓存
    }
}
