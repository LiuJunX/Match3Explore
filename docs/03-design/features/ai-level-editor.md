# 编辑器功能：AI 对话式关卡编辑

| 文档状态 | 作者 | 日期 | 对应版本 |
| :--- | :--- | :--- | :--- |
| **Implemented** | AI Assistant | 2026-01-21 | v2.0 |

## 1. 概述 (Overview)

AI 对话式关卡编辑允许用户通过自然语言描述来创建和修改关卡，无需手动操作复杂的 UI 控件。

### 1.1 功能定位

| 用途 | 描述 |
| :--- | :--- |
| **快速原型** | 通过描述快速生成关卡草稿 |
| **批量操作** | "把整个第一行都设为冰块" 等批量修改 |
| **参数调整** | 自然语言调整步数、目标等参数 |
| **关卡分析** | 调用分析工具获取胜率、瓶颈等指标 |
| **学习辅助** | 新手可通过对话了解关卡设计 |

### 1.2 核心特性

- **Function Calling**：原生工具调用，可靠性高于 JSON 解析
- **自然语言理解**：支持中文自然语言输入
- **上下文感知**：AI 了解当前关卡状态
- **分析集成**：可直接调用关卡分析和深度分析
- **可插拔架构**：支持多种 LLM 提供商（DeepSeek/OpenAI/Claude）

## 2. 系统架构

### 2.1 架构图 (v2.0 Function Calling)

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI 对话式关卡编辑系统 v2.0                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              Match3.Web (Blazor)                         │   │
│   │  ┌───────────────────────────────────────────────────┐  │   │
│   │  │  AIChatPanel.razor                                │  │   │
│   │  │  • 浮动聊天窗口 UI                                 │  │   │
│   │  │  • 消息列表展示                                    │  │   │
│   │  │  • 输入框和发送按钮                                │  │   │
│   │  └───────────────────────────────────────────────────┘  │   │
│   │                         │                                │   │
│   │                         ▼                                │   │
│   │  ┌───────────────────────────────────────────────────┐  │   │
│   │  │  WebLevelAIChatService                            │  │   │
│   │  │  • Function Calling 工具调用循环                   │  │   │
│   │  │  • 工具结果 → LevelIntent 转换                    │  │   │
│   │  │  • 分析工具直接执行                                │  │   │
│   │  └───────────────────────────────────────────────────┘  │   │
│   │                         │                                │   │
│   │         ┌───────────────┼───────────────┐               │   │
│   │         ▼               ▼               ▼               │   │
│   │  ┌────────────┐ ┌────────────┐ ┌────────────────────┐  │   │
│   │  │ToolRegistry│ │ ILLMClient │ │ Analysis Services  │  │   │
│   │  │ 18个工具   │ │ (可插拔)   │ │ • LevelAnalysis    │  │   │
│   │  │ 定义      │ │            │ │ • DeepAnalysis     │  │   │
│   │  └────────────┘ └────────────┘ └────────────────────┘  │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ═══════════════════════════════════════════════════════════   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              Match3.Editor (.NET Standard 2.1)           │   │
│   │  ┌───────────────────────────────────────────────────┐  │   │
│   │  │  LevelAIChatViewModel                             │  │   │
│   │  │  • 对话状态管理                                    │  │   │
│   │  │  • 消息历史                                        │  │   │
│   │  │  • 发送/接收消息                                   │  │   │
│   │  └───────────────────────────────────────────────────┘  │   │
│   │                         │                                │   │
│   │                         ▼                                │   │
│   │  ┌───────────────────────────────────────────────────┐  │   │
│   │  │  IntentExecutor                                   │  │   │
│   │  │  • 解析 LevelIntent                               │  │   │
│   │  │  • 调用 ViewModel/GridManipulator 执行操作        │  │   │
│   │  └───────────────────────────────────────────────────┘  │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Function Calling 流程

```
用户输入: "分析一下这个关卡，然后把网格改成 10x10"
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  WebLevelAIChatService.SendMessageAsync()                       │
├─────────────────────────────────────────────────────────────────┤
│  Round 1:                                                       │
│  ┌─────────────────┐    ┌─────────────────────────────────────┐│
│  │ LLM API Call    │───▶│ Response: tool_calls                ││
│  │ + tools[]       │    │ • analyze_level {}                  ││
│  │ + messages[]    │    │ • set_grid_size {width:10,height:10}││
│  └─────────────────┘    └─────────────────────────────────────┘│
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 处理工具调用:                                                ││
│  │ • analyze_level → 执行分析，返回结果文本                     ││
│  │ • set_grid_size → 转换为 LevelIntent，收集到 allIntents     ││
│  └─────────────────────────────────────────────────────────────┘│
│           │                                                     │
│           ▼                                                     │
│  Round 2:                                                       │
│  ┌─────────────────┐    ┌─────────────────────────────────────┐│
│  │ LLM API Call    │───▶│ Response: content (最终回复)         ││
│  │ + tool_results  │    │ "已分析完成并调整了网格大小..."       ││
│  └─────────────────┘    └─────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
返回: { message, intents: [SetGridSize], analysisResults }
```

### 2.3 文件清单

| 层 | 文件 | 职责 |
| :--- | :--- | :--- |
| **Editor** | `Interfaces/ILevelAIChatService.cs` | AI 服务接口定义 |
| **Editor** | `Models/AIChatModels.cs` | 数据模型（LevelContext, AIChatResponse, LevelIntent） |
| **Editor** | `Models/ChatMessage.cs` | 聊天消息模型 |
| **Editor** | `Logic/IntentExecutor.cs` | 意图执行器 |
| **Editor** | `Logic/LevelContextBuilder.cs` | 关卡上下文构建器 |
| **Editor** | `ViewModels/LevelAIChatViewModel.cs` | 对话 ViewModel |
| **Web** | `Services/AI/LLMOptions.cs` | LLM 配置选项 |
| **Web** | `Services/AI/ILLMClient.cs` | LLM 客户端接口 |
| **Web** | `Services/AI/FunctionCallingModels.cs` | **[新] Function Calling 数据模型** |
| **Web** | `Services/AI/ToolRegistry.cs` | **[新] 工具定义注册表** |
| **Web** | `Services/AI/OpenAICompatibleClient.cs` | OpenAI 兼容客户端 |
| **Web** | `Services/AI/WebLevelAIChatService.cs` | AI 服务实现 |
| **Web** | `Components/.../AIChatPanel.razor` | 聊天 UI 组件 |

### 2.4 接口定义

```csharp
// ILLMClient.cs - 支持 Function Calling
public interface ILLMClient
{
    bool IsAvailable { get; }

    Task<LLMResponse> SendAsync(
        IReadOnlyList<LLMMessage> messages,
        CancellationToken cancellationToken = default);

    Task<LLMResponse> SendWithToolsAsync(
        IReadOnlyList<LLMMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> SendStreamAsync(
        IReadOnlyList<LLMMessage> messages,
        CancellationToken cancellationToken = default);
}
```

```csharp
// LLMMessage - 支持工具调用
public class LLMMessage
{
    public string Role { get; set; }
    public string? Content { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }      // assistant 消息
    public string? ToolCallId { get; set; }              // tool 消息

    public static LLMMessage System(string content);
    public static LLMMessage User(string content);
    public static LLMMessage Assistant(string content);
    public static LLMMessage AssistantWithToolCalls(List<ToolCall> toolCalls);
    public static LLMMessage Tool(string toolCallId, string content);
}
```

## 3. 工具定义

### 3.1 编辑工具 (15个)

| 工具名 | 参数 | 描述 |
| :--- | :--- | :--- |
| `set_grid_size` | width, height | 设置网格大小 (3-12) |
| `set_move_limit` | moves | 设置步数限制 (1-99) |
| `set_objective` | index?, layer, element_type, count | 设置/添加目标 |
| `add_objective` | layer, element_type, count | 添加新目标 |
| `remove_objective` | index | 移除目标 |
| `paint_tile` | x, y, tile_type, bomb_type? | 绘制单个格子 |
| `paint_tile_region` | x1, y1, x2, y2, tile_type, bomb_type? | 绘制区域 |
| `paint_cover` | x, y, cover_type | 放置覆盖物 |
| `paint_cover_region` | x1, y1, x2, y2, cover_type | 区域覆盖物 |
| `paint_ground` | x, y, ground_type | 放置地面 |
| `paint_ground_region` | x1, y1, x2, y2, ground_type | 区域地面 |
| `place_bomb` | x, y, bomb_type, tile_type? | 放置炸弹 (-1 表示中心) |
| `generate_random_level` | - | 随机生成关卡 |
| `clear_region` | x1, y1, x2, y2 | 清空区域 |
| `clear_all` | - | 清空整个网格 |

### 3.2 分析工具 (3个)

| 工具名 | 参数 | 描述 |
| :--- | :--- | :--- |
| `analyze_level` | simulation_count? | 快速分析：胜率、死锁率、平均步数 |
| `deep_analyze` | simulations_per_tier? | 深度分析：分层胜率、技能敏感度、挫败风险等 |
| `get_bottleneck` | - | 瓶颈分析：最难目标和失败原因 |

### 3.3 可用元素

| 类型 | 可选值 |
| :--- | :--- |
| **tile_type** | Red, Green, Blue, Yellow, Purple, Orange, Rainbow, None |
| **bomb_type** | None, Horizontal, Vertical, Color, Ufo, Square5x5 |
| **cover_type** | None, Cage, Chain, Bubble |
| **ground_type** | None, Ice |
| **layer** | Tile, Cover, Ground |
| **element_type** | 整数索引 (见下表) |

#### element_type 索引对照

| Layer | 索引 | 元素 |
| :--- | :--- | :--- |
| Tile | 0 | Red |
| Tile | 1 | Green |
| Tile | 2 | Blue |
| Tile | 3 | Yellow |
| Tile | 4 | Purple |
| Tile | 5 | Orange |
| Tile | 6 | Rainbow |
| Cover | 0 | Cage |
| Cover | 1 | Chain |
| Cover | 2 | Bubble |
| Ground | 0 | Ice |

## 4. 用例示例

### 4.1 基础编辑

**用户**: 把网格改成 10x10

**AI 工具调用**:
```json
{
  "name": "set_grid_size",
  "arguments": {"width": 10, "height": 10}
}
```

**AI 回复**: 已将网格大小调整为 10x10。

---

**用户**: 目标是消除 30 个红色方块和 20 个蓝色方块

**AI 工具调用**:
```json
[
  {"name": "set_objective", "arguments": {"layer": "Tile", "element_type": 0, "count": 30}},
  {"name": "add_objective", "arguments": {"layer": "Tile", "element_type": 2, "count": 20}}
]
```

**AI 回复**: 已设置目标：消除 30 个红色方块和 20 个蓝色方块。

---

**用户**: 在中间放一个彩虹炸弹

**AI 工具调用**:
```json
{
  "name": "place_bomb",
  "arguments": {"x": -1, "y": -1, "bomb_type": "Color", "tile_type": "Rainbow"}
}
```

**AI 回复**: 已在网格中心放置彩虹炸弹。

### 4.2 区域操作

**用户**: 第一行全部放冰块

**AI 工具调用**:
```json
{
  "name": "paint_ground_region",
  "arguments": {"x1": 0, "y1": 0, "x2": 7, "y2": 0, "ground_type": "Ice"}
}
```

---

**用户**: 左下角 3x3 区域放笼子

**AI 工具调用**:
```json
{
  "name": "paint_cover_region",
  "arguments": {"x1": 0, "y1": 5, "x2": 2, "y2": 7, "cover_type": "Cage"}
}
```

### 4.3 关卡分析

**用户**: 分析一下这个关卡

**AI 工具调用**:
```json
{
  "name": "analyze_level",
  "arguments": {"simulation_count": 500}
}
```

**AI 回复**:
```
## 关卡分析结果
- 模拟次数: 500
- 胜率: 68.2%
- 死锁率: 3.4%
- 平均使用步数: 17.3
- 难度评级: Medium
- 分析耗时: 1234ms
```

---

**用户**: 做一个深度分析

**AI 工具调用**:
```json
{
  "name": "deep_analyze",
  "arguments": {"simulations_per_tier": 250}
}
```

**AI 回复**:
```
## 深度分析结果

### 分层胜率
- Beginner: 82.4%
- Average: 67.2%
- Expert: 51.6%

### 关键指标
- 技能敏感度: 31% (高=技能关, 低=运气关)
- 运气依赖度: 28% (理想范围: 20-40%)
- 挫败风险: 12.3% (连续3局失败概率)
- P95通关次数: 4次 (95%玩家在此次数内通关)

### 瓶颈目标
- 最难目标: Tile-Blue (占失败的 45%)
```

---

**用户**: 瓶颈在哪里？

**AI 工具调用**:
```json
{
  "name": "get_bottleneck",
  "arguments": {}
}
```

**AI 回复**:
```
## 瓶颈分析
- 瓶颈目标: Tile-Blue
- 失败占比: 45%

### 建议
- 该目标是主要失败原因，建议降低目标数量或增加步数
```

### 4.4 复合操作

**用户**: 帮我设计一个简单的新手关：8x8 网格，25 步，目标是消除 20 个红色方块

**AI 工具调用**:
```json
[
  {"name": "set_grid_size", "arguments": {"width": 8, "height": 8}},
  {"name": "set_move_limit", "arguments": {"moves": 25}},
  {"name": "set_objective", "arguments": {"layer": "Tile", "element_type": 0, "count": 20}},
  {"name": "generate_random_level", "arguments": {}}
]
```

**AI 回复**: 已创建新手关卡：8x8 网格，25 步，目标消除 20 个红色方块，并随机生成了初始布局。

## 5. 用户界面

### 5.1 浮动聊天窗口

- **位置**：右侧固定浮动
- **宽度**：360px
- **组件**：消息列表 + 输入区域
- **折叠**：点击按钮可显示/隐藏

### 5.2 消息类型

| 类型 | 样式 | 说明 |
| :--- | :--- | :--- |
| 用户消息 | 蓝色背景，右对齐 | 用户输入 |
| AI 回复 | 深色背景，左对齐 | AI 响应文本 |
| 分析结果 | Markdown 格式 | 分析工具输出 |
| 错误消息 | 红色边框 | 请求失败或取消 |
| 意图标签 | 小徽章 | 显示执行的操作 |

### 5.3 交互状态

| 状态 | UI 表现 |
| :--- | :--- |
| 空闲 | 输入框可用，发送按钮启用 |
| 等待响应 | 输入框禁用，显示加载动画 |
| AI 不可用 | 显示警告提示 |

## 6. 配置

### 6.1 LLM 配置选项

```json
// appsettings.json
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

### 6.2 支持 Function Calling 的提供商

| Provider | BaseUrl | Function Calling 支持 |
| :--- | :--- | :--- |
| DeepSeek | `https://api.deepseek.com/v1` | ✅ 支持 |
| OpenAI | `https://api.openai.com/v1` | ✅ 支持 |
| Azure OpenAI | `https://xxx.openai.azure.com/` | ✅ 支持 |
| Ollama (部分模型) | `http://localhost:11434/v1` | ⚠️ 需要支持 tools 的模型 |

## 7. 错误处理

| 错误类型 | 处理方式 |
| :--- | :--- |
| 网络错误 | 显示错误消息，允许重试 |
| API 密钥无效 | 提示检查配置 |
| 工具调用参数解析失败 | 返回错误到 LLM，让其重试 |
| 分析服务未配置 | 返回友好提示 |
| 达到最大轮数 | 返回已收集的 intents |

## 8. 扩展性

### 8.1 添加新工具

1. 在 `ToolRegistry.cs` 添加工具定义方法
2. 在 `GetAllTools()` 添加调用
3. 如果是编辑工具，在 `ToolNameToIntentType` 添加映射
4. 如果是分析工具，在 `AnalysisToolNames` 添加名称
5. 在 `WebLevelAIChatService` 添加执行逻辑（分析工具）

### 8.2 添加新的 LLM 提供商

1. 实现 `ILLMClient` 接口
2. 实现 `SendWithToolsAsync()` 方法
3. 在 `Program.cs` 注册服务
4. 配置 `appsettings.json`

## 9. 测试

### 9.1 单元测试覆盖

| 测试类 | 测试内容 |
| :--- | :--- |
| `WebLevelAIChatServiceTests` | 工具调用解析、Intent 转换、参数转换 |
| `ToolRegistry` 测试 | 工具数量、映射完整性、分析工具识别 |

### 9.2 集成测试场景

| 场景 | 验证内容 |
| :--- | :--- |
| "把网格改成 10x10" | `set_grid_size` 被调用，参数正确 |
| "分析一下这个关卡" | `analyze_level` 被调用，返回分析结果 |
| "在中心放一个彩虹炸弹" | `place_bomb` 参数正确，center 处理正确 |
| 多工具调用 | 多个工具按顺序执行，intents 都被收集 |

## 10. 版本历史

| 版本 | 日期 | 变更内容 |
| :--- | :--- | :--- |
| v1.0 | 2026-01-20 | 初始实现：JSON 响应解析，14 种意图类型 |
| v2.0 | 2026-01-21 | **重构为 Function Calling**：原生工具调用，新增 3 个分析工具，共 18 个工具 |
