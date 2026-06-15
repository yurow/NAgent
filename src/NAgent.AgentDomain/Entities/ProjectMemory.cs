namespace NAgent.AgentDomain.Entities;

/// <summary>
/// 项目长期记忆实体 - 跨会话持久化的知识
/// </summary>
public class ProjectMemory
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Content { get; private set; }
    public string Summary { get; private set; }
    public MemoryCategory Category { get; private set; }
    public MemoryImportance Importance { get; private set; }
    public int AccessCount { get; private set; }
    public DateTime LastAccessedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private ProjectMemory() { }

    public ProjectMemory(
        Guid projectId,
        string content,
        string summary,
        MemoryCategory category,
        MemoryImportance importance,
        DateTime? expiresAt = null)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Summary = summary ?? content;
        Category = category;
        Importance = importance;
        AccessCount = 0;
        LastAccessedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(1);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    public void UpdateContent(string content, string? summary = null)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Summary = summary ?? content;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSummary(string summary)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImportance(MemoryImportance importance)
    {
        Importance = importance;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
    }

    public void SetExpiration(DateTime expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// 计算记忆得分（用于排序和淘汰）
    /// </summary>
    public double CalculateScore()
    {
        var importanceWeight = (int)Importance;
        var accessWeight = Math.Min(AccessCount, 100) * 0.1;
        var recencyWeight = (DateTime.UtcNow - LastAccessedAt).TotalDays < 7 ? 10 : 0;
        return importanceWeight + accessWeight + recencyWeight;
    }
}

/// <summary>
/// 记忆分类
/// </summary>
public enum MemoryCategory
{
    /// <summary>
    /// 通用记忆
    /// </summary>
    General = 0,

    /// <summary>
    /// 用户偏好
    /// </summary>
    UserPreference = 1,

    /// <summary>
    /// 项目知识
    /// </summary>
    ProjectKnowledge = 2,

    /// <summary>
    /// 代码模式
    /// </summary>
    CodePattern = 3,

    /// <summary>
    /// 错误和解决方案
    /// </summary>
    ErrorSolution = 4,

    /// <summary>
    /// 决策记录
    /// </summary>
    Decision = 5,

    /// <summary>
    /// 任务上下文
    /// </summary>
    TaskContext = 6
}

/// <summary>
/// 记忆重要性等级
/// </summary>
public enum MemoryImportance
{
    /// <summary>
    /// 低 - 可随时淘汰
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中 - 正常保留
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高 - 优先保留
    /// </summary>
    High = 3,

    /// <summary>
    /// 关键 - 永不淘汰
    /// </summary>
    Critical = 4
}
