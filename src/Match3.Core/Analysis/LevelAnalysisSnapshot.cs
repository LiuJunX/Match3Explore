using System;
using System.Collections.Generic;

namespace Match3.Core.Analysis;

/// <summary>
/// 关卡分析快照 - 用于存储到文件的统一结构
/// 文件命名: {levelName}.analysis.json
/// </summary>
public sealed class LevelAnalysisSnapshot
{
    /// <summary>快照版本号</summary>
    public int Version { get; set; } = 1;

    /// <summary>分析时间 (UTC)</summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>关联的关卡文件名</summary>
    public string LevelFileName { get; set; } = "";

    /// <summary>基础分析结果</summary>
    public BasicAnalysisData? Basic { get; set; }

    /// <summary>深度分析结果 (可选)</summary>
    public DeepAnalysisData? Deep { get; set; }
}

/// <summary>
/// 基础分析数据 (可序列化)
/// </summary>
public sealed class BasicAnalysisData
{
    /// <summary>总模拟次数</summary>
    public int TotalSimulations { get; set; }

    /// <summary>通过率 (0-1)</summary>
    public float WinRate { get; set; }

    /// <summary>死锁率 (0-1)</summary>
    public float DeadlockRate { get; set; }

    /// <summary>步数用尽率 (0-1)</summary>
    public float OutOfMovesRate { get; set; }

    /// <summary>平均使用步数</summary>
    public float AverageMovesUsed { get; set; }

    /// <summary>平均得分</summary>
    public float AverageScore { get; set; }

    /// <summary>难度评级</summary>
    public string DifficultyRating { get; set; } = "";

    /// <summary>分析耗时(毫秒)</summary>
    public double ElapsedMs { get; set; }

    /// <summary>
    /// 从 LevelAnalysisResult 创建
    /// </summary>
    public static BasicAnalysisData FromResult(LevelAnalysisResult result)
    {
        return new BasicAnalysisData
        {
            TotalSimulations = result.TotalSimulations,
            WinRate = result.WinRate,
            DeadlockRate = result.DeadlockRate,
            OutOfMovesRate = result.TotalSimulations > 0
                ? (float)result.OutOfMovesCount / result.TotalSimulations
                : 0,
            AverageMovesUsed = result.AverageMovesUsed,
            AverageScore = result.AverageScore,
            DifficultyRating = result.DifficultyRating.ToString(),
            ElapsedMs = result.ElapsedMs
        };
    }
}

/// <summary>
/// 深度分析数据 (可序列化)
/// </summary>
public sealed class DeepAnalysisData
{
    /// <summary>心流曲线 (每步爽感值)</summary>
    public float[] FlowCurve { get; set; } = Array.Empty<float>();

    /// <summary>心流最小值</summary>
    public float FlowMin { get; set; }

    /// <summary>心流最大值</summary>
    public float FlowMax { get; set; }

    /// <summary>心流平均值</summary>
    public float FlowAverage { get; set; }

    /// <summary>分层胜率 (Tier名称 -> 胜率)</summary>
    public Dictionary<string, float> TierWinRates { get; set; } = new();

    /// <summary>瓶颈目标描述</summary>
    public string BottleneckObjective { get; set; } = "";

    /// <summary>瓶颈目标失败率</summary>
    public float BottleneckFailureRate { get; set; }

    /// <summary>技能敏感度 (0-1)</summary>
    public float SkillSensitivity { get; set; }

    /// <summary>挫败风险 (0-1)</summary>
    public float FrustrationRisk { get; set; }

    /// <summary>运气依赖度 (0-1)</summary>
    public float LuckDependency { get; set; }

    /// <summary>P95 通关尝试次数</summary>
    public int P95ClearAttempts { get; set; }

    /// <summary>总模拟次数</summary>
    public int TotalSimulations { get; set; }

    /// <summary>分析耗时(毫秒)</summary>
    public double ElapsedMs { get; set; }

    /// <summary>
    /// 从 DeepAnalysisResult 创建
    /// </summary>
    public static DeepAnalysisData FromResult(DeepAnalysisResult result)
    {
        return new DeepAnalysisData
        {
            FlowCurve = result.FlowCurve,
            FlowMin = result.FlowMin,
            FlowMax = result.FlowMax,
            FlowAverage = result.FlowAverage,
            TierWinRates = new Dictionary<string, float>(result.TierWinRates),
            BottleneckObjective = result.BottleneckObjective ?? "",
            BottleneckFailureRate = result.BottleneckFailureRate,
            SkillSensitivity = result.SkillSensitivity,
            FrustrationRisk = result.FrustrationRisk,
            LuckDependency = result.LuckDependency,
            P95ClearAttempts = result.P95ClearAttempts,
            TotalSimulations = result.TotalSimulations,
            ElapsedMs = result.ElapsedMs
        };
    }

    /// <summary>
    /// 转换为 DeepAnalysisResult (用于 UI 显示)
    /// </summary>
    public DeepAnalysisResult ToResult()
    {
        return new DeepAnalysisResult
        {
            FlowCurve = FlowCurve,
            FlowMin = FlowMin,
            FlowMax = FlowMax,
            FlowAverage = FlowAverage,
            TierWinRates = new Dictionary<string, float>(TierWinRates),
            BottleneckObjective = BottleneckObjective,
            BottleneckFailureRate = BottleneckFailureRate,
            SkillSensitivity = SkillSensitivity,
            FrustrationRisk = FrustrationRisk,
            LuckDependency = LuckDependency,
            P95ClearAttempts = P95ClearAttempts,
            TotalSimulations = TotalSimulations,
            ElapsedMs = ElapsedMs,
            WasCancelled = false
        };
    }
}
