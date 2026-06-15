using NAgent.AgentDomain.Enums;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// Tool 定义实体 - 通过 YAML 配置描述的工具
/// </summary>
public class ToolDefinition
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public ToolSecurityLevel SecurityLevel { get; private set; }
    public string YamlContent { get; private set; }
    public string FilePath { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// 工具参数定义
    /// </summary>
    public List<ToolParameter> Parameters { get; private set; } = new();

    /// <summary>
    /// 工具执行配置
    /// </summary>
    public ToolExecutionConfig ExecutionConfig { get; private set; } = new();

    private ToolDefinition() { }

    public ToolDefinition(string name, string description, string category, string yamlContent, string filePath)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? "general";
        YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        SecurityLevel = ToolSecurityLevel.Low;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
        Parameters = new List<ToolParameter>();
        ExecutionConfig = new ToolExecutionConfig();
    }

    public void UpdateYamlContent(string yamlContent)
    {
        YamlContent = yamlContent ?? throw new ArgumentNullException(nameof(yamlContent));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSecurityLevel(ToolSecurityLevel level)
    {
        SecurityLevel = level;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddParameter(ToolParameter parameter)
    {
        Parameters.Add(parameter ?? throw new ArgumentNullException(nameof(parameter)));
    }

    public void SetExecutionConfig(ToolExecutionConfig config)
    {
        ExecutionConfig = config ?? throw new ArgumentNullException(nameof(config));
    }
}

/// <summary>
/// 工具参数定义
/// </summary>
public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool Required { get; set; } = true;
    public object? DefaultValue { get; set; }
    public List<string>? EnumValues { get; set; }
}

/// <summary>
/// 工具执行配置
/// </summary>
public class ToolExecutionConfig
{
    /// <summary>
    /// 执行类型: local, sandbox, http, command
    /// </summary>
    public string ExecutionType { get; set; } = "local";

    /// <summary>
    /// 执行命令或脚本路径
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// HTTP 端点（当 ExecutionType 为 http 时）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// HTTP 方法: GET, POST, PUT, DELETE
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 工作目录
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}
