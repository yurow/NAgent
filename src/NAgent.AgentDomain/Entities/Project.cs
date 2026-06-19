using SqlSugar;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// 项目实体 - 用户的工作项目
/// </summary>
[SugarTable("projects")]
public class Project
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", Length = 2000, IsNullable = true)]
    public string Description { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public Guid UserId { get; set; }

    [SugarColumn(ColumnName = "workspace_path", Length = 500, IsNullable = false)]
    public string WorkspacePath { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_active", IsNullable = false)]
    public bool IsActive { get; set; }

    [SugarColumn(ColumnName = "active_role_id", IsNullable = true)]
    public Guid? ActiveRoleId { get; set; }

    [SugarColumn(ColumnName = "last_accessed_at", IsNullable = true)]
    public DateTime? LastAccessedAt { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; }

    [SugarColumn(ColumnName = "updated_at", IsNullable = false)]
    public DateTime UpdatedAt { get; set; }

    [SugarColumn(IsIgnore = true)]
    public List<AgentSession> Sessions { get; set; } = new();

    public Project()
    {
        Sessions = new List<AgentSession>();
    }

    public static Project Create(string name, string description, Guid userId, string workspacePath, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("项目名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("工作空间路径不能为空", nameof(workspacePath));

        var project = new Project
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            UserId = userId,
            WorkspacePath = workspacePath,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Sessions = new List<AgentSession>()
        };

        return project;
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("项目名称不能为空", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActiveRole(Guid? roleId)
    {
        ActiveRoleId = roleId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSession(AgentSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        Sessions.Add(session);
        UpdatedAt = DateTime.UtcNow;
    }
}
