using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Match3.Core.Analysis;
using Match3.Core.Config;
using Match3.Core.Models.Enums;
using Match3.Editor.Interfaces;
using Match3.Editor.Models;
using Microsoft.Extensions.Logging;

namespace Match3.Web.Services.AI
{
    /// <summary>
    /// AI 关卡编辑服务的 Web 实现 - 使用 Function Calling
    /// </summary>
    public class WebLevelAIChatService : ILevelAIChatService
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<WebLevelAIChatService> _logger;
        private readonly ILevelAnalysisService? _analysisService;
        private readonly DeepAnalysisService? _deepAnalysisService;
        private readonly Func<LevelConfig>? _getLevelConfig;

        private const int MaxToolCallRounds = 5;

        public bool IsAvailable => _llmClient.IsAvailable;

        public WebLevelAIChatService(
            ILLMClient llmClient,
            ILogger<WebLevelAIChatService> logger,
            ILevelAnalysisService? analysisService = null,
            DeepAnalysisService? deepAnalysisService = null,
            Func<LevelConfig>? getLevelConfig = null)
        {
            _llmClient = llmClient;
            _logger = logger;
            _analysisService = analysisService;
            _deepAnalysisService = deepAnalysisService;
            _getLevelConfig = getLevelConfig;
        }

        public async Task<AIChatResponse> SendMessageAsync(
            string message,
            LevelContext context,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            var messages = BuildMessages(message, context, history);
            var tools = ToolRegistry.GetAllTools();
            var allIntents = new List<LevelIntent>();
            var analysisResults = new StringBuilder();

            int round = 0;
            while (round < MaxToolCallRounds)
            {
                round++;
                _logger.LogDebug("Tool calling round {Round}", round);

                var response = await _llmClient.SendWithToolsAsync(messages, tools, cancellationToken);

                if (!response.Success)
                {
                    return new AIChatResponse
                    {
                        Success = false,
                        Error = response.Error
                    };
                }

                // 没有工具调用，返回最终结果
                if (!response.HasToolCalls)
                {
                    var finalMessage = response.Content ?? "";
                    if (analysisResults.Length > 0)
                    {
                        finalMessage = analysisResults.ToString() + (string.IsNullOrEmpty(finalMessage) ? "" : "\n\n" + finalMessage);
                    }

                    return new AIChatResponse
                    {
                        Success = true,
                        Message = finalMessage,
                        Intents = allIntents
                    };
                }

                // 处理工具调用
                var toolResults = new List<ToolResult>();

                foreach (var toolCall in response.ToolCalls!)
                {
                    var toolName = toolCall.Function.Name;
                    var arguments = toolCall.Function.Arguments;

                    _logger.LogDebug("Processing tool call: {ToolName} with arguments: {Arguments}", toolName, arguments);

                    if (ToolRegistry.AnalysisToolNames.Contains(toolName))
                    {
                        // 分析工具
                        var result = await ExecuteAnalysisToolAsync(toolName, arguments, cancellationToken);
                        toolResults.Add(new ToolResult
                        {
                            ToolCallId = toolCall.Id,
                            Content = result
                        });
                        analysisResults.AppendLine(result);
                    }
                    else if (ToolRegistry.ToolNameToIntentType.TryGetValue(toolName, out var intentType))
                    {
                        // 编辑工具
                        var intent = ConvertToLevelIntent(intentType, arguments);
                        if (intent != null)
                        {
                            allIntents.Add(intent);
                            toolResults.Add(new ToolResult
                            {
                                ToolCallId = toolCall.Id,
                                Content = $"已执行 {toolName}"
                            });
                        }
                        else
                        {
                            toolResults.Add(new ToolResult
                            {
                                ToolCallId = toolCall.Id,
                                Content = $"参数解析失败: {arguments}"
                            });
                        }
                    }
                    else
                    {
                        toolResults.Add(new ToolResult
                        {
                            ToolCallId = toolCall.Id,
                            Content = $"未知工具: {toolName}"
                        });
                    }
                }

                // 添加助手消息和工具结果到对话
                messages.Add(LLMMessage.AssistantWithToolCalls(response.ToolCalls));
                foreach (var result in toolResults)
                {
                    messages.Add(LLMMessage.Tool(result.ToolCallId, result.Content));
                }
            }

            // 达到最大轮数
            return new AIChatResponse
            {
                Success = true,
                Message = analysisResults.Length > 0 ? analysisResults.ToString() : "操作已完成",
                Intents = allIntents
            };
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(
            string message,
            LevelContext context,
            IReadOnlyList<ChatMessage> history,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 流式模式暂不支持工具调用，降级为普通调用
            var response = await SendMessageAsync(message, context, history, cancellationToken);
            if (response.Success && !string.IsNullOrEmpty(response.Message))
            {
                yield return response.Message;
            }
            else if (!response.Success)
            {
                yield return $"[错误: {response.Error}]";
            }
        }

        private List<LLMMessage> BuildMessages(
            string userMessage,
            LevelContext context,
            IReadOnlyList<ChatMessage> history)
        {
            var messages = new List<LLMMessage>
            {
                LLMMessage.System(BuildSystemPrompt(context))
            };

            // 添加历史消息（最多 10 条）
            var startIndex = Math.Max(0, history.Count - 10);
            for (int i = startIndex; i < history.Count; i++)
            {
                var msg = history[i];
                if (msg.Role == ChatRole.User)
                    messages.Add(LLMMessage.User(msg.Content));
                else if (msg.Role == ChatRole.Assistant && !msg.IsError)
                    messages.Add(LLMMessage.Assistant(msg.Content));
            }

            // 添加当前用户消息
            messages.Add(LLMMessage.User(userMessage));

            return messages;
        }

        private string BuildSystemPrompt(LevelContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine("你是一个 Match3 消除游戏的关卡编辑助手。根据用户的自然语言描述，使用工具来编辑关卡或分析关卡。");
            sb.AppendLine();
            sb.AppendLine("## 当前关卡状态");
            sb.AppendLine($"- 网格大小: {context.Width} x {context.Height}");
            sb.AppendLine($"- 步数限制: {context.MoveLimit}");

            if (context.Objectives != null && context.Objectives.Length > 0)
            {
                sb.AppendLine("- 当前目标:");
                for (int i = 0; i < context.Objectives.Length; i++)
                {
                    var obj = context.Objectives[i];
                    if (obj.TargetLayer != ObjectiveTargetLayer.None)
                    {
                        sb.AppendLine($"  [{i}] {obj.TargetLayer} - {obj.ElementType} x {obj.TargetCount}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(context.GridSummary))
                sb.AppendLine($"- 网格摘要: {context.GridSummary}");

            if (!string.IsNullOrEmpty(context.DifficultyText))
                sb.AppendLine($"- 难度评估: {context.DifficultyText}");

            sb.AppendLine();
            sb.AppendLine("## 可用元素");
            sb.AppendLine("- TileType: Red, Green, Blue, Yellow, Purple, Orange, Rainbow, None");
            sb.AppendLine("- BombType: None, Horizontal, Vertical, Color, Ufo, Square5x5");
            sb.AppendLine("- CoverType: None, Cage, Chain, Bubble");
            sb.AppendLine("- GroundType: None, Ice");
            sb.AppendLine();
            sb.AppendLine("## 注意事项");
            sb.AppendLine("- 坐标从 (0,0) 开始，x 是列，y 是行");
            sb.AppendLine("- place_bomb 的 x/y 参数使用 -1 表示网格中心");
            sb.AppendLine("- 如果用户只是聊天或提问，直接回复文字即可，不需要调用工具");
            sb.AppendLine("- 用户要求分析关卡时，使用 analyze_level 或 deep_analyze 工具");

            return sb.ToString();
        }

        private LevelIntent? ConvertToLevelIntent(LevelIntentType type, string argumentsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;
                var parameters = new Dictionary<string, object>();

                foreach (var prop in root.EnumerateObject())
                {
                    var key = ConvertSnakeToCamel(prop.Name);
                    object value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? i : prop.Value.GetDouble(),
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                    parameters[key] = value;
                }

                // 特殊处理 place_bomb: x=-1 或 y=-1 表示中心位置
                if (type == LevelIntentType.PlaceBomb)
                {
                    bool xIsCenter = parameters.TryGetValue("x", out var xVal) && IsNegativeOne(xVal);
                    bool yIsCenter = parameters.TryGetValue("y", out var yVal) && IsNegativeOne(yVal);
                    if (xIsCenter || yIsCenter)
                    {
                        parameters["center"] = true;
                    }
                }

                return new LevelIntent
                {
                    Type = type,
                    Parameters = parameters
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse tool arguments: {Arguments}", argumentsJson);
                return null;
            }
        }

        private static string ConvertSnakeToCamel(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            var parts = snakeCase.Split('_');
            if (parts.Length == 1)
                return snakeCase;

            var sb = new StringBuilder(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    sb.Append(char.ToUpperInvariant(parts[i][0]));
                    if (parts[i].Length > 1)
                        sb.Append(parts[i].Substring(1));
                }
            }
            return sb.ToString();
        }

        private static bool IsNegativeOne(object? value)
        {
            if (value == null) return false;
            try
            {
                return Convert.ToInt32(value) == -1;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ExecuteAnalysisToolAsync(
            string toolName,
            string argumentsJson,
            CancellationToken cancellationToken)
        {
            // 获取当前关卡配置
            var levelConfig = _getLevelConfig?.Invoke();
            if (levelConfig == null)
            {
                return "无法获取当前关卡配置";
            }

            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;

                switch (toolName)
                {
                    case "analyze_level":
                        return await ExecuteAnalyzeLevelAsync(levelConfig, root, cancellationToken);

                    case "deep_analyze":
                        return await ExecuteDeepAnalyzeAsync(levelConfig, root, cancellationToken);

                    case "get_bottleneck":
                        return await ExecuteGetBottleneckAsync(levelConfig, cancellationToken);

                    default:
                        return $"未知分析工具: {toolName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing analysis tool: {ToolName}", toolName);
                return $"分析执行失败: {ex.Message}";
            }
        }

        private async Task<string> ExecuteAnalyzeLevelAsync(
            LevelConfig levelConfig,
            JsonElement args,
            CancellationToken cancellationToken)
        {
            if (_analysisService == null)
            {
                return "分析服务未配置";
            }

            int simulationCount = 500;
            if (args.TryGetProperty("simulation_count", out var simCountProp))
            {
                simulationCount = simCountProp.GetInt32();
            }

            var config = new AnalysisConfig
            {
                SimulationCount = simulationCount,
                UseParallel = true
            };

            var result = await _analysisService.AnalyzeAsync(levelConfig, config, null, cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("## 关卡分析结果");
            sb.AppendLine($"- 模拟次数: {result.TotalSimulations}");
            sb.AppendLine($"- 胜率: {result.WinRate:P1}");
            sb.AppendLine($"- 死锁率: {result.DeadlockRate:P1}");
            sb.AppendLine($"- 平均使用步数: {result.AverageMovesUsed:F1}");
            sb.AppendLine($"- 难度评级: {result.DifficultyRating}");
            sb.AppendLine($"- 分析耗时: {result.ElapsedMs:F0}ms");

            return sb.ToString();
        }

        private async Task<string> ExecuteDeepAnalyzeAsync(
            LevelConfig levelConfig,
            JsonElement args,
            CancellationToken cancellationToken)
        {
            if (_deepAnalysisService == null)
            {
                return "深度分析服务未配置";
            }

            int simulationsPerTier = 250;
            if (args.TryGetProperty("simulations_per_tier", out var simPerTierProp))
            {
                simulationsPerTier = simPerTierProp.GetInt32();
            }

            var result = await _deepAnalysisService.AnalyzeAsync(levelConfig, simulationsPerTier, null, cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("## 深度分析结果");
            sb.AppendLine();
            sb.AppendLine("### 分层胜率");
            foreach (var kvp in result.TierWinRates)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value:P1}");
            }
            sb.AppendLine();
            sb.AppendLine("### 关键指标");
            sb.AppendLine($"- 技能敏感度: {result.SkillSensitivity:P0} (高=技能关, 低=运气关)");
            sb.AppendLine($"- 运气依赖度: {result.LuckDependency:P0} (理想范围: 20-40%)");
            sb.AppendLine($"- 挫败风险: {result.FrustrationRisk:P1} (连续3局失败概率)");
            sb.AppendLine($"- P95通关次数: {result.P95ClearAttempts}次 (95%玩家在此次数内通关)");
            sb.AppendLine();
            sb.AppendLine("### 瓶颈目标");
            if (!string.IsNullOrEmpty(result.BottleneckObjective))
            {
                sb.AppendLine($"- 最难目标: {result.BottleneckObjective} (占失败的 {result.BottleneckFailureRate:P0})");
            }
            else
            {
                sb.AppendLine("- 无明显瓶颈目标");
            }
            sb.AppendLine();
            sb.AppendLine("### 心流曲线");
            sb.AppendLine($"- 最低爽感: {result.FlowMin:F1}");
            sb.AppendLine($"- 最高爽感: {result.FlowMax:F1}");
            sb.AppendLine($"- 平均爽感: {result.FlowAverage:F1}");
            sb.AppendLine();
            sb.AppendLine($"分析耗时: {result.ElapsedMs:F0}ms, 总模拟次数: {result.TotalSimulations}");

            return sb.ToString();
        }

        private async Task<string> ExecuteGetBottleneckAsync(
            LevelConfig levelConfig,
            CancellationToken cancellationToken)
        {
            // 使用深度分析获取瓶颈信息
            if (_deepAnalysisService == null)
            {
                return "深度分析服务未配置，无法获取瓶颈信息";
            }

            var result = await _deepAnalysisService.AnalyzeAsync(levelConfig, 100, null, cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("## 瓶颈分析");

            if (!string.IsNullOrEmpty(result.BottleneckObjective))
            {
                sb.AppendLine($"- 瓶颈目标: {result.BottleneckObjective}");
                sb.AppendLine($"- 失败占比: {result.BottleneckFailureRate:P0}");
                sb.AppendLine();
                sb.AppendLine("### 建议");
                if (result.BottleneckFailureRate > 0.5f)
                {
                    sb.AppendLine("- 该目标是主要失败原因，建议降低目标数量或增加步数");
                }
                else
                {
                    sb.AppendLine("- 目标难度相对均衡");
                }
            }
            else
            {
                sb.AppendLine("- 无明显瓶颈目标，各目标难度相对均衡");
            }

            sb.AppendLine();
            sb.AppendLine($"### 分层胜率参考");
            foreach (var kvp in result.TierWinRates)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value:P1}");
            }

            return sb.ToString();
        }
    }
}
