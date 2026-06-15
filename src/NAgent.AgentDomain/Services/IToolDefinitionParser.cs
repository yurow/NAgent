using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Services;

/// <summary>
/// Tool YAML 配置解析器接口
/// </summary>
public interface IToolDefinitionParser
{
    /// <summary>
    /// 从 YAML 内容解析 Tool 定义
    /// </summary>
    ToolDefinition Parse(string yamlContent, string filePath);

    /// <summary>
    /// 从文件解析 Tool 定义
    /// </summary>
    Task<ToolDefinition> ParseFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证 YAML 内容是否符合 Tool 规范
    /// </summary>
    bool Validate(string yamlContent, out string? errorMessage);
}
