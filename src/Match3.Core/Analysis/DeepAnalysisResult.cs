using System;
using System.Collections.Generic;

namespace Match3.Core.Analysis;

/// <summary>
/// Deep Analysis 结果 - 7个高级指标
/// </summary>
public sealed class DeepAnalysisResult
{
    // === 1. 心流曲线 ===

    /// <summary>
    /// 每步爽感分数数组
    /// </summary>
    public float[] FlowCurve { get; init; } = Array.Empty<float>();

    /// <summary>
    /// 心流最小值
    /// </summary>
    public float FlowMin { get; init; }

    /// <summary>
    /// 心流最大值
    /// </summary>
    public float FlowMax { get; init; }

    /// <summary>
    /// 心流平均值
    /// </summary>
    public float FlowAverage { get; init; }

    // === 2. 分层胜率 ===

    /// <summary>
    /// 四层玩家胜率 { "Novice": 0.2, "Casual": 0.5, "Core": 0.7, "Expert": 0.85 }
    /// </summary>
    public Dictionary<string, float> TierWinRates { get; init; } = new();

    // === 3. 瓶颈目标 ===

    /// <summary>
    /// 最常导致失败的目标名称
    /// </summary>
    public string BottleneckObjective { get; init; } = "";

    /// <summary>
    /// 瓶颈目标导致失败的比例
    /// </summary>
    public float BottleneckFailureRate { get; init; }

    // === 4. 技能敏感度 ===

    /// <summary>
    /// (Expert胜率 - Novice胜率) / Expert胜率
    /// 高值表示技能关，低值表示运气关
    /// </summary>
    public float SkillSensitivity { get; init; }

    // === 5. 挫败风险 ===

    /// <summary>
    /// P(连续失败 >= 3 局)
    /// </summary>
    public float FrustrationRisk { get; init; }

    // === 6. 运气依赖度 ===

    /// <summary>
    /// 局间方差 / 总方差
    /// 0% = 纯技能关, 100% = 纯运气关
    /// 理想范围: 20-40%
    /// </summary>
    public float LuckDependency { get; init; }

    // === 7. P95 通关次数 ===

    /// <summary>
    /// 95%玩家能在N次内通关
    /// </summary>
    public int P95ClearAttempts { get; init; }

    // === 元数据 ===

    /// <summary>
    /// 分析耗时(毫秒)
    /// </summary>
    public double ElapsedMs { get; init; }

    /// <summary>
    /// 总模拟次数
    /// </summary>
    public int TotalSimulations { get; init; }

    /// <summary>
    /// 是否被取消
    /// </summary>
    public bool WasCancelled { get; init; }
}

/// <summary>
/// Deep Analysis 进度报告
/// </summary>
public sealed class DeepAnalysisProgress
{
    /// <summary>
    /// 进度百分比 0.0 - 1.0
    /// </summary>
    public float Progress { get; init; }

    /// <summary>
    /// 已完成的模拟次数
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    /// 总模拟次数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 当前阶段描述
    /// </summary>
    public string Stage { get; init; } = "";
}
