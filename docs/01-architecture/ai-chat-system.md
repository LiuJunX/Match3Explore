# AI 对话式编辑系统架构

## Overview

AI 对话式编辑系统允许用户通过自然语言与关卡编辑器交互。系统采用**分层架构**和**意图驱动执行**模式，将 LLM 交互、意图解析、操作执行分离到不同层次，实现关注点分离和平台无关性。

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           User Interface                                 │
│                      (AIChatPanel.razor)                                │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ 用户输入
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         Match3.Web Layer                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   WebLevelAIChatService                          │   │
│  │  • 实现 ILevelAIChatService 接口                                 │   │
│  │  • 构建系统提示词 (包含关卡上下文)                               │   │
│  │  • Function Calling 工具调用循环                                 │   │
│  │  • 分析工具直接执行                                              │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│         ┌───────────────────┼───────────────────┐                      │
│         ▼                   ▼                   ▼                      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────────┐        │
│  │ToolRegistry │    │ ILLMClient  │    │ Analysis Services   │        │
│  │ 18 个工具   │    │  (可插拔)   │    │ • LevelAnalysis     │        │
│  │ 定义       │    │             │    │ • DeepAnalysis      │        │
│  └─────────────┘    └──────┬──────┘    └─────────────────────┘        │
│                            │                                           │
│                    ┌───────┴───────┐                                   │
│                    ▼               ▼                                   │
│             ┌───────────┐   ┌───────────┐                             │
│             │ DeepSeek  │   │  OpenAI   │   ... 更多提供商            │
│             │  Client   │   │  Client   │                             │
│             └───────────┘   └───────────┘                             │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ AIChatResponse { Message, Intents[] }
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Match3.Editor Layer                               │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   LevelAIChatViewModel                           │   │
│  │  • 对话状态管理 (消息历史、发送状态)                             │   │
│  │  • 调用 ILevelAIChatService                                     │   │
│  │  • 将 Intents 交给 IntentExecutor                               │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                     IntentExecutor                               │   │
│  │  • 解析 LevelIntent 参数                                        │   │
│  │  • 调用 LevelEditorViewModel / GridManipulator                  │   │
│  │  • 执行实际的关卡修改                                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              LevelEditorViewModel + GridManipulator              │   │
│  │  • 关卡配置修改                                                 │   │
│  │  • 网格操作                                                     │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

## 1. 设计原则

### 1.1 分层职责

| 层 | 职责 | 依赖 |
|:---|:-----|:-----|
| **Web** | LLM 通信、工具定义、提示词构建 | Editor 接口 |
| **Editor** | 意图执行、状态管理、UI 逻辑 | Core 模型 |
| **Core** | 关卡配置、分析服务 | 无外部依赖 |

### 1.2 关键设计决策

| 决策 | 原因 |
|:-----|:-----|
| **接口定义在 Editor 层** | Editor 是核心业务层，Web 是实现层 |
| **LLM 客户端可插拔** | 支持多提供商、便于测试 |
| **Intent 作为中间表示** | 解耦 LLM 输出格式与执行逻辑 |
| **分析工具在 Web 层执行** | 需要访问 LLM 进行多轮对话 |

## 2. 核心组件

### 2.1 接口层 (Match3.Editor)

```csharp
// 服务接口 - 平台无关
public interface ILevelAIChatService
{
    bool IsAvailable { get; }

    Task<AIChatResponse> SendMessageAsync(
        string message,
        LevelContext context,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);
}

// 响应模型
public class AIChatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }           // AI 回复文本
    public IReadOnlyList<LevelIntent> Intents;    // 操作意图列表
    public string? Error { get; set; }
}

// 意图模型 - LLM 输出与执行逻辑的桥梁
public class LevelIntent
{
    public LevelIntentType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

### 2.2 LLM 客户端层 (Match3.Web)

```csharp
// 可插拔的 LLM 客户端接口
public interface ILLMClient
{
    bool IsAvailable { get; }

    // 普通对话
    Task<LLMResponse> SendAsync(
        IReadOnlyList<LLMMessage> messages,
        CancellationToken cancellationToken = default);

    // Function Calling (v2.0)
    Task<LLMResponse> SendWithToolsAsync(
        IReadOnlyList<LLMMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

// 消息模型 - 支持工具调用
public class LLMMessage
{
    public string Role { get; set; }              // system/user/assistant/tool
    public string? Content { get; set; }
    public List<ToolCall>? ToolCalls { get; set; } // assistant 响应
    public string? ToolCallId { get; set; }        // tool 结果
}
```

### 2.3 工具注册表 (Match3.Web)

```csharp
public static class ToolRegistry
{
    // 工具名 → Intent 类型映射
    public static readonly Dictionary<string, LevelIntentType> ToolNameToIntentType;

    // 分析工具集合 (不转换为 Intent，直接执行)
    public static readonly HashSet<string> AnalysisToolNames;

    // 获取所有工具定义
    public static List<ToolDefinition> GetAllTools();
}
```

## 3. 数据流

### 3.1 编辑操作流程

```
用户: "把网格改成 10x10"
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  1. LevelAIChatViewModel.SendMessageAsync()                 │
│     • 构建 LevelContext (当前关卡状态)                      │
│     • 调用 ILevelAIChatService                              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  2. WebLevelAIChatService.SendMessageAsync()                │
│     • 构建系统提示词 + 历史消息                              │
│     • 调用 ILLMClient.SendWithToolsAsync()                  │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  3. LLM API (DeepSeek/OpenAI)                               │
│     • 返回 tool_calls: [{name: "set_grid_size", args: ...}] │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  4. WebLevelAIChatService - 工具处理                         │
│     • 查找 ToolNameToIntentType["set_grid_size"]            │
│     • 转换参数: snake_case → camelCase                       │
│     • 创建 LevelIntent { Type: SetGridSize, Parameters }    │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  5. IntentExecutor.Execute(intent)                          │
│     • 解析参数: width=10, height=10                         │
│     • 调用 ViewModel.ResizeGrid(10, 10)                     │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 分析操作流程

```
用户: "分析一下这个关卡"
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  1-3. 同上，LLM 返回 tool_calls: [{name: "analyze_level"}]  │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  4. WebLevelAIChatService - 分析工具处理                     │
│     • 检测: AnalysisToolNames.Contains("analyze_level")     │
│     • 调用 ILevelAnalysisService.AnalyzeAsync()             │
│     • 格式化结果为 Markdown 文本                             │
│     • 作为 tool_result 返回给 LLM                           │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  5. LLM API - 第二轮                                        │
│     • 收到分析结果，生成最终回复                             │
│     • 返回 content: "关卡分析完成，胜率 68%..."              │
└────────────────────────┬────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  6. 返回 AIChatResponse                                     │
│     • Message: 分析结果 + AI 总结                            │
│     • Intents: [] (分析操作无 Intent)                       │
└─────────────────────────────────────────────────────────────┘
```

## 4. LLM 交互策略

### 4.1 策略对比

| 策略 | 实现方式 | 优点 | 缺点 |
|:-----|:---------|:-----|:-----|
| **JSON 解析 (v1.0)** | 提示词要求输出 JSON，解析响应 | 所有 LLM 支持 | 格式不稳定，解析易失败 |
| **Function Calling (v2.0)** | 使用 tools API，LLM 原生调用 | 类型安全，多轮支持 | 需要提供商支持 |
| **结构化输出** | JSON Schema 约束输出 | 格式保证 | 不支持多轮交互 |

### 4.2 Function Calling 优势

```
JSON 解析方式:
┌──────────────────────────────────────────────────────────────┐
│  Prompt: "请以 JSON 格式输出: {message, intents: [...]}"     │
│                              ↓                               │
│  LLM 输出: "好的，我来帮你设置。```json{...}```"              │
│                              ↓                               │
│  解析: 提取 JSON → 可能失败 (格式错误、缺字段、类型错误)      │
└──────────────────────────────────────────────────────────────┘

Function Calling 方式:
┌──────────────────────────────────────────────────────────────┐
│  Request: messages + tools: [{name, parameters, required}]   │
│                              ↓                               │
│  LLM 响应: tool_calls: [{id, name, arguments: {...}}]        │
│                              ↓                               │
│  解析: 结构化数据，参数已验证 (类型、枚举、范围)              │
└──────────────────────────────────────────────────────────────┘
```

### 4.3 多轮工具调用

```
Round 1: 用户请求 → LLM 返回 tool_calls
         ↓
         执行工具，收集结果
         ↓
Round 2: tool_results → LLM 返回 tool_calls 或 content
         ↓
         ... (最多 MaxToolCallRounds 轮)
         ↓
Final:   返回最终 content + 所有收集的 Intents
```

## 5. 工具分类

### 5.1 编辑工具 (15个)

转换为 `LevelIntent`，由 `IntentExecutor` 执行。

| 类别 | 工具 |
|:-----|:-----|
| 网格 | `set_grid_size`, `clear_region`, `clear_all` |
| 关卡 | `set_move_limit`, `generate_random_level` |
| 目标 | `set_objective`, `add_objective`, `remove_objective` |
| 方块 | `paint_tile`, `paint_tile_region`, `place_bomb` |
| 覆盖物 | `paint_cover`, `paint_cover_region` |
| 地面 | `paint_ground`, `paint_ground_region` |

### 5.2 分析工具 (3个)

在 `WebLevelAIChatService` 中直接执行，结果返回给 LLM。

| 工具 | 服务 | 输出 |
|:-----|:-----|:-----|
| `analyze_level` | `ILevelAnalysisService` | 胜率、死锁率、难度 |
| `deep_analyze` | `DeepAnalysisService` | 分层胜率、技能敏感度、瓶颈 |
| `get_bottleneck` | `DeepAnalysisService` | 瓶颈目标、建议 |

## 6. 扩展点

### 6.1 添加新的 LLM 提供商

```csharp
// 1. 实现 ILLMClient
public class ClaudeClient : ILLMClient
{
    public Task<LLMResponse> SendWithToolsAsync(...) { /* Claude API */ }
}

// 2. 注册服务 (Program.cs)
builder.Services.AddHttpClient<ILLMClient, ClaudeClient>();
```

### 6.2 添加新的编辑工具

```csharp
// 1. ToolRegistry.cs - 添加工具定义
private static ToolDefinition CreateNewTool() => new() { ... };

// 2. ToolRegistry.cs - 添加映射
ToolNameToIntentType["new_tool"] = LevelIntentType.NewTool;

// 3. LevelIntentType.cs - 添加枚举值
public enum LevelIntentType { ..., NewTool }

// 4. IntentExecutor.cs - 添加执行逻辑
case LevelIntentType.NewTool:
    ExecuteNewTool(intent, config);
    break;
```

### 6.3 添加新的分析工具

```csharp
// 1. ToolRegistry.cs - 添加工具定义
private static ToolDefinition CreateNewAnalysisTool() => new() { ... };

// 2. ToolRegistry.cs - 添加到分析集合
AnalysisToolNames = new() { ..., "new_analysis" };

// 3. WebLevelAIChatService.cs - 添加执行逻辑
case "new_analysis":
    return await ExecuteNewAnalysisAsync(levelConfig, args, ct);
```

## 7. 配置

### 7.1 LLM 配置

```json
{
  "LLM": {
    "Provider": "DeepSeek",
    "BaseUrl": "https://api.deepseek.com/v1",
    "ApiKey": "sk-xxx",
    "Model": "deepseek-chat",
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

### 7.2 服务注册

```csharp
// Program.cs
builder.Services.Configure<LLMOptions>(
    builder.Configuration.GetSection("LLM"));

builder.Services.AddHttpClient<ILLMClient, OpenAICompatibleClient>();

builder.Services.AddScoped<ILevelAIChatService, WebLevelAIChatService>();
```

## 8. 测试策略

### 8.1 单元测试

| 层 | 测试内容 | Mock |
|:---|:---------|:-----|
| WebLevelAIChatService | 工具调用解析、Intent 转换 | MockLLMClient |
| ToolRegistry | 工具定义完整性、映射正确性 | 无 |
| IntentExecutor | 参数解析、边界条件 | MockViewModel |

### 8.2 Mock 策略

```csharp
// MockLLMClient - 模拟工具调用响应
public class MockLLMClient : ILLMClient
{
    private readonly List<ToolCall>? _toolCalls;

    public Task<LLMResponse> SendWithToolsAsync(...)
    {
        // 第一次调用返回 tool_calls
        // tool_result 后返回 final content
    }
}
```

## 9. 相关文档

| 文档 | 内容 |
|:-----|:-----|
| `docs/03-design/features/ai-level-editor.md` | 功能规格、用例示例 |
| `docs/02-guides/llm-configuration.md` | 配置指南 |
| `docs/04-adr/0009-ai-function-calling.md` | Function Calling 决策记录 |
