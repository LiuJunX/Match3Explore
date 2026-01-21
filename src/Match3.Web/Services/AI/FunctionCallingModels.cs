using System.Collections.Generic;

namespace Match3.Web.Services.AI
{
    /// <summary>
    /// 工具定义 - OpenAI Function Calling 格式
    /// </summary>
    public class ToolDefinition
    {
        public string Type { get; set; } = "function";
        public FunctionDefinition Function { get; set; } = new();
    }

    /// <summary>
    /// 函数定义
    /// </summary>
    public class FunctionDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public FunctionParameters Parameters { get; set; } = new();
    }

    /// <summary>
    /// 函数参数定义
    /// </summary>
    public class FunctionParameters
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, ParameterProperty> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
    }

    /// <summary>
    /// 参数属性定义
    /// </summary>
    public class ParameterProperty
    {
        public string Type { get; set; } = "string";
        public string? Description { get; set; }
        public List<string>? Enum { get; set; }
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }

    /// <summary>
    /// 工具调用 - LLM 响应中的工具调用请求
    /// </summary>
    public class ToolCall
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "function";
        public FunctionCall Function { get; set; } = new();
    }

    /// <summary>
    /// 函数调用详情
    /// </summary>
    public class FunctionCall
    {
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "{}";
    }

    /// <summary>
    /// 工具调用结果 - 发送给 LLM 的工具执行结果
    /// </summary>
    public class ToolResult
    {
        public string ToolCallId { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
