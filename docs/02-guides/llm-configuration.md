# LLM 配置指南

本指南说明如何配置 AI 对话式关卡编辑器的 LLM 后端。

> **v2.1 更新**: AI 编辑器支持深度思考模式（DeepSeek R1），可通过 ReasonerModel 配置启用。

## 快速开始

### 1. 配置文件位置

编辑 `src/Match3.Web/appsettings.Development.json`（开发环境）或 `appsettings.json`（生产环境）：

```json
{
  "LLM": {
    "Provider": "DeepSeek",
    "BaseUrl": "https://api.deepseek.com/v1",
    "ApiKey": "your-api-key-here",
    "Model": "deepseek-chat",
    "ReasonerModel": "deepseek-reasoner",
    "ReasonerMaxTokens": 1024,
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

### 2. 获取 API Key

| 提供商 | 获取方式 |
| :--- | :--- |
| DeepSeek | https://platform.deepseek.com/api_keys |
| OpenAI | https://platform.openai.com/api-keys |
| Azure OpenAI | Azure Portal → OpenAI 资源 → 密钥 |

## Function Calling 支持

AI 编辑器 v2.0 使用 Function Calling（工具调用）来执行操作，这比 JSON 解析更可靠。

### 支持情况

| 提供商 | Function Calling | 推荐模型 |
| :--- | :--- | :--- |
| DeepSeek | ✅ 支持 | deepseek-chat |
| OpenAI | ✅ 支持 | gpt-4o-mini, gpt-4o |
| Azure OpenAI | ✅ 支持 | 按部署配置 |
| Ollama | ⚠️ 部分支持 | qwen2.5:7b (需确认版本) |

### 可用工具

系统提供 19 个工具供 AI 调用：

- **编辑工具 (15个)**: `set_grid_size`, `set_move_limit`, `set_objective`, `add_objective`, `remove_objective`, `paint_tile`, `paint_tile_region`, `paint_cover`, `paint_cover_region`, `paint_ground`, `paint_ground_region`, `place_bomb`, `generate_random_level`, `clear_region`, `clear_all`
- **分析工具 (3个)**: `analyze_level`, `deep_analyze`, `get_bottleneck`
- **路由工具 (1个)**: `need_deep_thinking` - 触发深度思考模式

详细工具定义见 [AI 关卡编辑器文档](../03-design/features/ai-level-editor.md)。

## 支持的提供商

### DeepSeek（默认）

```json
{
  "LLM": {
    "Provider": "DeepSeek",
    "BaseUrl": "https://api.deepseek.com/v1",
    "ApiKey": "sk-xxx",
    "Model": "deepseek-chat"
  }
}
```

**特点**：
- 性价比高
- 中文支持优秀
- API 格式与 OpenAI 兼容

### OpenAI

```json
{
  "LLM": {
    "Provider": "OpenAI",
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "sk-xxx",
    "Model": "gpt-4o-mini"
  }
}
```

**可用模型**：
- `gpt-4o` - 最强，成本较高
- `gpt-4o-mini` - 平衡性价比（推荐）
- `gpt-4-turbo` - 较快响应

### Azure OpenAI

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "BaseUrl": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key",
    "Model": "your-deployment-name"
  }
}
```

**注意**：Model 填写 Azure 中的部署名称，不是模型名称。

### 本地部署（Ollama）

```json
{
  "LLM": {
    "Provider": "Local",
    "BaseUrl": "http://localhost:11434/v1",
    "ApiKey": "ollama",
    "Model": "qwen2.5:7b"
  }
}
```

**前提**：
1. 安装 Ollama: https://ollama.ai
2. 拉取模型: `ollama pull qwen2.5:7b`
3. 启动服务: `ollama serve`

## 配置参数说明

| 参数 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `Provider` | string | "DeepSeek" | 提供商名称（用于日志） |
| `BaseUrl` | string | - | API 基础 URL |
| `ApiKey` | string | - | API 密钥 |
| `Model` | string | "deepseek-chat" | 模型名称 |
| `ReasonerModel` | string | null | 推理模型（深度思考），为空则禁用 |
| `ReasonerMaxTokens` | int | 1024 | 推理模型最大输出 token 数 |
| `MaxTokens` | int | 2048 | 最大输出 token 数 |
| `Temperature` | float | 0.7 | 创造性（0.0-1.0） |

### Temperature 建议

| 值 | 效果 | 适用场景 |
| :--- | :--- | :--- |
| 0.0-0.3 | 确定性高 | 精确的参数设置 |
| 0.5-0.7 | 平衡（推荐） | 日常编辑任务 |
| 0.8-1.0 | 创造性高 | 关卡创意生成 |

## 安全注意事项

### 不要提交 API Key

1. 使用 `appsettings.Development.json`（已在 .gitignore）
2. 或使用环境变量：

```bash
# Windows PowerShell
$env:LLM__ApiKey = "sk-xxx"

# Linux/macOS
export LLM__ApiKey="sk-xxx"
```

### 生产环境配置

使用 Azure Key Vault 或其他密钥管理服务：

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

## 故障排除

### 问题：API 返回 401 Unauthorized

**原因**：API Key 无效或过期

**解决**：
1. 检查 ApiKey 是否正确复制
2. 在提供商控制台确认密钥状态
3. 确认账户有足够余额

### 问题：连接超时

**原因**：网络问题或 API 服务不可用

**解决**：
1. 检查网络连接
2. 确认 BaseUrl 正确
3. 查看提供商状态页面

### 问题：AI 不可用（IsAvailable = false）

**原因**：配置缺失或无效

**解决**：
1. 检查 `appsettings.json` 中是否有 LLM 配置节
2. 确认 ApiKey 不为空
3. 重启应用

### 问题：响应内容为空或无法解析

**原因**：模型输出格式不符合预期

**解决**：
1. 尝试降低 Temperature 值
2. 使用更强的模型（如 gpt-4o）
3. 检查 MaxTokens 是否足够

## 测试配置

启动应用后，在关卡编辑器中：

1. 点击右上角 "AI 助手" 按钮
2. 输入测试消息："你好"
3. 如果收到响应，配置成功

如果显示 "AI 服务不可用"，检查配置和 API Key。

## 成本估算

| 提供商 | 模型 | 估算成本 |
| :--- | :--- | :--- |
| DeepSeek | deepseek-chat | ~$0.001/请求 |
| OpenAI | gpt-4o-mini | ~$0.002/请求 |
| OpenAI | gpt-4o | ~$0.03/请求 |
| 本地 | qwen2.5:7b | 免费（需硬件） |

*成本取决于消息长度和历史上下文，以上为典型单次请求估算。*
