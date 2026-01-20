using System;
using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Core.Utility;

namespace Match3.Core.Analysis.MCTS;

/// <summary>
/// MCTS 树节点
/// </summary>
public sealed class MCTSNode
{
    /// <summary>父节点</summary>
    public MCTSNode? Parent { get; }

    /// <summary>到达此节点的移动</summary>
    public ValidMove? Move { get; }

    /// <summary>子节点列表</summary>
    public List<MCTSNode>? Children { get; private set; }

    /// <summary>未尝试的移动</summary>
    public List<ValidMove>? UntriedMoves { get; private set; }

    /// <summary>访问次数</summary>
    public int VisitCount { get; private set; }

    /// <summary>累计奖励（胜利次数）</summary>
    public float TotalReward { get; private set; }

    /// <summary>是否为终端节点</summary>
    public bool IsTerminal { get; private set; }

    /// <summary>终端状态类型</summary>
    public TerminalState TerminalType { get; private set; }

    /// <summary>节点深度</summary>
    public int Depth { get; }

    /// <summary>
    /// 平均奖励值
    /// </summary>
    public float AverageReward => VisitCount > 0 ? TotalReward / VisitCount : 0;

    /// <summary>
    /// 创建根节点
    /// </summary>
    public MCTSNode()
    {
        Parent = null;
        Move = null;
        Depth = 0;
    }

    /// <summary>
    /// 创建子节点
    /// </summary>
    public MCTSNode(MCTSNode parent, ValidMove move)
    {
        Parent = parent;
        Move = move;
        Depth = parent.Depth + 1;
    }

    /// <summary>
    /// 初始化未尝试的移动列表
    /// </summary>
    public void InitializeUntriedMoves(List<ValidMove> validMoves)
    {
        UntriedMoves = new List<ValidMove>(validMoves);
        Children = new List<MCTSNode>();
    }

    /// <summary>
    /// 标记为终端节点
    /// </summary>
    public void MarkAsTerminal(TerminalState state)
    {
        IsTerminal = true;
        TerminalType = state;
        UntriedMoves = null;
        Children = null;
    }

    /// <summary>
    /// 是否完全展开
    /// </summary>
    public bool IsFullyExpanded => UntriedMoves == null || UntriedMoves.Count == 0;

    /// <summary>
    /// 是否有子节点
    /// </summary>
    public bool HasChildren => Children != null && Children.Count > 0;

    /// <summary>
    /// 展开一个子节点
    /// </summary>
    public MCTSNode Expand(int moveIndex)
    {
        if (UntriedMoves == null || moveIndex >= UntriedMoves.Count)
            throw new InvalidOperationException("No untried moves available");

        var move = UntriedMoves[moveIndex];
        UntriedMoves.RemoveAt(moveIndex);

        var child = new MCTSNode(this, move);
        Children!.Add(child);
        return child;
    }

    /// <summary>
    /// 使用 UCB1 选择最佳子节点
    /// </summary>
    /// <param name="explorationConstant">探索常数 C，通常为 sqrt(2)</param>
    public MCTSNode SelectBestChild(float explorationConstant = 1.414f)
    {
        if (Children == null || Children.Count == 0)
            throw new InvalidOperationException("No children to select from");

        MCTSNode? best = null;
        float bestUcb = float.MinValue;

        float logParentVisits = MathF.Log(VisitCount);

        foreach (var child in Children)
        {
            if (child.VisitCount == 0)
            {
                // 未访问的节点优先
                return child;
            }

            // UCB1 = 平均奖励 + C * sqrt(ln(父访问次数) / 子访问次数)
            float exploitation = child.AverageReward;
            float exploration = explorationConstant * MathF.Sqrt(logParentVisits / child.VisitCount);
            float ucb = exploitation + exploration;

            if (ucb > bestUcb)
            {
                bestUcb = ucb;
                best = child;
            }
        }

        return best!;
    }

    /// <summary>
    /// 选择访问次数最多的子节点（用于最终决策）
    /// </summary>
    public MCTSNode SelectMostVisitedChild()
    {
        if (Children == null || Children.Count == 0)
            throw new InvalidOperationException("No children to select from");

        MCTSNode? best = null;
        int bestVisits = -1;

        foreach (var child in Children)
        {
            if (child.VisitCount > bestVisits)
            {
                bestVisits = child.VisitCount;
                best = child;
            }
        }

        return best!;
    }

    /// <summary>
    /// 选择平均奖励最高的子节点
    /// </summary>
    public MCTSNode SelectBestRewardChild()
    {
        if (Children == null || Children.Count == 0)
            throw new InvalidOperationException("No children to select from");

        MCTSNode? best = null;
        float bestReward = float.MinValue;

        foreach (var child in Children)
        {
            if (child.VisitCount > 0 && child.AverageReward > bestReward)
            {
                bestReward = child.AverageReward;
                best = child;
            }
        }

        return best ?? Children[0];
    }

    /// <summary>
    /// 反向传播结果
    /// </summary>
    public void Backpropagate(float reward)
    {
        var node = this;
        while (node != null)
        {
            node.VisitCount++;
            node.TotalReward += reward;
            node = node.Parent;
        }
    }

    /// <summary>
    /// 获取从根到此节点的路径
    /// </summary>
    public List<ValidMove> GetPathFromRoot()
    {
        var path = new List<ValidMove>();
        var node = this;

        while (node.Parent != null && node.Move.HasValue)
        {
            path.Add(node.Move.Value);
            node = node.Parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// 获取节点统计信息
    /// </summary>
    public string GetStats()
    {
        return $"Visits: {VisitCount}, Reward: {TotalReward:F2}, Avg: {AverageReward:F3}";
    }
}

/// <summary>
/// 终端状态类型
/// </summary>
public enum TerminalState
{
    None,
    Win,
    Deadlock,
    OutOfMoves
}
