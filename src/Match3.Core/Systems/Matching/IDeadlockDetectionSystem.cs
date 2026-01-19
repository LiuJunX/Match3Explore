using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Core.Utility;

namespace Match3.Core.Systems.Matching;

/// <summary>
/// 死锁检测系统接口
/// 负责检测棋盘是否存在可行的移动
/// </summary>
public interface IDeadlockDetectionSystem
{
    /// <summary>
    /// 检查棋盘是否存在至少一个有效移动
    /// </summary>
    /// <param name="state">游戏状态</param>
    /// <returns>如果存在有效移动则返回 true，否则返回 false（死锁）</returns>
    bool HasValidMoves(in GameState state);

    /// <summary>
    /// 查找所有有效移动（用于调试、测试、AI）
    /// </summary>
    /// <param name="state">游戏状态</param>
    /// <returns>所有有效移动的列表，调用者负责释放</returns>
    List<ValidMove> FindAllValidMoves(in GameState state);

    /// <summary>
    /// 使缓存失效（预留接口，用于未来优化）
    /// </summary>
    void InvalidateCache();
}
