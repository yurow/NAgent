using SqlSugar;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// 角色实体 - 项目级别的 AI 角色/人设配置
/// </summary>
[SugarTable("agent_roles")]
public class AgentRole
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public Guid ProjectId { get; set; }

    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true)]
    public string Description { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "system_prompt", Length = 8000, IsNullable = false)]
    public string SystemPrompt { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "model_id", IsNullable = true)]
    public Guid? ModelId { get; set; }

    [SugarColumn(ColumnName = "is_active", IsNullable = false)]
    public bool IsActive { get; set; } = true;

    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at", IsNullable = false)]
    public DateTime UpdatedAt { get; set; }

    public AgentRole()
    {
    }

    /// <summary>
    /// 工厂方法 - 创建角色
    /// </summary>
    public static AgentRole Create(Guid projectId, string name, string description,
        string systemPrompt, Guid? modelId = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("角色名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(systemPrompt))
            throw new ArgumentException("System Prompt 不能为空", nameof(systemPrompt));

        return new AgentRole
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Description = description ?? string.Empty,
            SystemPrompt = systemPrompt,
            ModelId = modelId,
            IsActive = true,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新角色信息
    /// </summary>
    public void Update(string name, string description, string systemPrompt, Guid? modelId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("角色名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(systemPrompt))
            throw new ArgumentException("System Prompt 不能为空", nameof(systemPrompt));

        Name = name;
        Description = description ?? string.Empty;
        SystemPrompt = systemPrompt;
        ModelId = modelId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
