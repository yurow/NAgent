using NAgent.AgentDomain.Enums;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// Agent 工具实体
/// </summary>
public class AgentTool
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public ToolSecurityLevel SecurityLevel { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private AgentTool() { } // EF Core 需要

    public AgentTool(string name, string description, ToolSecurityLevel securityLevel)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        SecurityLevel = securityLevel;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新工具描述
    /// </summary>
    public void UpdateDescription(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 启用/禁用工具
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 判断是否需要在沙箱中执行
    /// </summary>
    public bool RequiresSandbox()
    {
        return SecurityLevel == ToolSecurityLevel.High;
    }
}
