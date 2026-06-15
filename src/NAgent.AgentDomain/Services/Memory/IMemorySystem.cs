namespace NAgent.AgentDomain.Services.Memory;

/// <summary>
/// 记忆条目
/// </summary>
public class MemoryEntry
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 会话记忆上下文
/// </summary>
public class SessionMemoryContext
{
    public Guid ProjectId { get; set; }
    public Guid SessionId { get; set; }
    public List<MemoryEntry> ShortTermMemory { get; set; } = new();
    public List<MemoryEntry> LongTermMemory { get; set; } = new();
    public Dictionary<string, object> ProjectContext { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 记忆存储接口
/// </summary>
public interface IMemoryStorage
{
    /// <summary>
    /// 保存记忆
    /// </summary>
    Task SaveAsync(Guid projectId, Guid sessionId, SessionMemoryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载记忆
    /// </summary>
    Task<SessionMemoryContext?> LoadAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除记忆
    /// </summary>
    Task DeleteAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索记忆
    /// </summary>
    Task<List<MemoryEntry>> SearchAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目所有会话记忆
    /// </summary>
    Task<List<SessionMemoryContext>> GetProjectMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 记忆系统接口 - 增强版，支持项目级长期记忆
/// </summary>
public interface IMemorySystem
{
    /// <summary>
    /// 添加短期记忆
    /// </summary>
    Task AddMemoryAsync(Guid projectId, Guid sessionId, string role, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取短期记忆
    /// </summary>
    Task<List<MemoryEntry>> GetShortTermMemoryAsync(Guid projectId, Guid sessionId, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取长期记忆
    /// </summary>
    Task<List<MemoryEntry>> GetLongTermMemoryAsync(Guid projectId, Guid sessionId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取完整记忆上下文
    /// </summary>
    Task<SessionMemoryContext> GetMemoryContextAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存记忆上下文
    /// </summary>
    Task SaveMemoryContextAsync(Guid projectId, Guid sessionId, SessionMemoryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除会话记忆
    /// </summary>
    Task ClearSessionMemoryAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索相关记忆
    /// </summary>
    Task<List<MemoryEntry>> SearchMemoriesAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新项目上下文
    /// </summary>
    Task UpdateProjectContextAsync(Guid projectId, Dictionary<string, object> context, CancellationToken cancellationToken = default);

    // ===== 项目级长期记忆（新增） =====

    /// <summary>
    /// 保存项目长期记忆
    /// </summary>
    Task SaveProjectLongTermMemoryAsync(Guid projectId, string content, string summary, int categoryId, int importance, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目长期记忆摘要（用于构建上下文）
    /// </summary>
    Task<List<ProjectMemorySummary>> GetProjectMemorySummaryAsync(Guid projectId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索项目长期记忆
    /// </summary>
    Task<List<ProjectMemorySummary>> SearchProjectLongTermMemoryAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除项目的所有记忆（包括会话和长期记忆）
    /// </summary>
    Task ClearProjectAllMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 项目记忆摘要（轻量级，用于传递给 LLM）
/// </summary>
public class ProjectMemorySummary
{
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Importance { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 记忆系统工厂接口
/// </summary>
public interface IMemorySystemFactory
{
    /// <summary>
    /// 创建记忆系统
    /// </summary>
    IMemorySystem CreateMemorySystem(string memoryType = "default");

    /// <summary>
    /// 获取可用的记忆系统类型
    /// </summary>
    IEnumerable<string> GetAvailableMemoryTypes();
}
