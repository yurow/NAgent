using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentDomain.Services.Memory;

/// <summary>
/// 默认记忆系统实现 - 支持项目级长短记忆隔离
/// </summary>
public class DefaultMemorySystem : IMemorySystem
{
    private readonly IMemoryStorage _storage;
    private readonly IProjectMemoryRepository _projectMemoryRepository;
    private const int ShortTermMemoryLimit = 20;
    private const int LongTermMemoryLimit = 100;

    public DefaultMemorySystem(IMemoryStorage storage, IProjectMemoryRepository projectMemoryRepository)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _projectMemoryRepository = projectMemoryRepository ?? throw new ArgumentNullException(nameof(projectMemoryRepository));
    }

    // ===== 会话级短期记忆 =====

    public async Task AddMemoryAsync(Guid projectId, Guid sessionId, string role, string content, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        var context = await GetMemoryContextAsync(projectId, sessionId, cancellationToken);

        var entry = new MemoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata
        };

        context.ShortTermMemory.Add(entry);

        // 溢出时自动转移到长期记忆
        if (context.ShortTermMemory.Count > ShortTermMemoryLimit)
        {
            var oldestEntry = context.ShortTermMemory[0];
            context.LongTermMemory.Add(oldestEntry);
            context.ShortTermMemory.RemoveAt(0);

            if (context.LongTermMemory.Count > LongTermMemoryLimit)
            {
                context.LongTermMemory.RemoveAt(0);
            }
        }

        context.LastUpdated = DateTime.UtcNow;
        await SaveMemoryContextAsync(projectId, sessionId, context, cancellationToken);
    }

    public async Task<List<MemoryEntry>> GetShortTermMemoryAsync(Guid projectId, Guid sessionId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var context = await _storage.LoadAsync(projectId, sessionId, cancellationToken);
        return context?.ShortTermMemory.TakeLast(limit).ToList() ?? new List<MemoryEntry>();
    }

    public async Task<List<MemoryEntry>> GetLongTermMemoryAsync(Guid projectId, Guid sessionId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var context = await _storage.LoadAsync(projectId, sessionId, cancellationToken);
        return context?.LongTermMemory.TakeLast(limit).ToList() ?? new List<MemoryEntry>();
    }

    public async Task<SessionMemoryContext> GetMemoryContextAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var context = await _storage.LoadAsync(projectId, sessionId, cancellationToken);

        if (context == null)
        {
            context = new SessionMemoryContext
            {
                ProjectId = projectId,
                SessionId = sessionId,
                ShortTermMemory = new List<MemoryEntry>(),
                LongTermMemory = new List<MemoryEntry>(),
                ProjectContext = new Dictionary<string, object>(),
                LastUpdated = DateTime.UtcNow
            };
        }

        return context;
    }

    public async Task SaveMemoryContextAsync(Guid projectId, Guid sessionId, SessionMemoryContext context, CancellationToken cancellationToken = default)
    {
        context.LastUpdated = DateTime.UtcNow;
        await _storage.SaveAsync(projectId, sessionId, context, cancellationToken);
    }

    public async Task ClearSessionMemoryAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _storage.DeleteAsync(projectId, sessionId, cancellationToken);
    }

    public async Task<List<MemoryEntry>> SearchMemoriesAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _storage.SearchAsync(projectId, query, limit, cancellationToken);
    }

    public async Task UpdateProjectContextAsync(Guid projectId, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        var memories = await _storage.GetProjectMemoriesAsync(projectId, cancellationToken);

        foreach (var memory in memories)
        {
            foreach (var kvp in context)
            {
                memory.ProjectContext[kvp.Key] = kvp.Value;
            }
            memory.LastUpdated = DateTime.UtcNow;
            await _storage.SaveAsync(projectId, memory.SessionId, memory, cancellationToken);
        }
    }

    // ===== 项目级长期记忆（新增） =====

    public async Task SaveProjectLongTermMemoryAsync(Guid projectId, string content, string summary, int categoryId, int importance, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        var category = (MemoryCategory)categoryId;
        var importanceLevel = (MemoryImportance)importance;

        var memory = new ProjectMemory(
            projectId,
            content,
            summary,
            category,
            importanceLevel);

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                memory.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        await _projectMemoryRepository.AddAsync(memory, cancellationToken);
    }

    public async Task<List<ProjectMemorySummary>> GetProjectMemorySummaryAsync(Guid projectId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var memories = await _projectMemoryRepository.GetTopMemoriesAsync(projectId, limit, cancellationToken);
        return memories.Select(m => new ProjectMemorySummary
        {
            Content = m.Content,
            Summary = m.Summary,
            Category = m.Category.ToString(),
            Importance = (int)m.Importance,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<List<ProjectMemorySummary>> SearchProjectLongTermMemoryAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        var memories = await _projectMemoryRepository.SearchAsync(projectId, query, limit, cancellationToken);
        return memories.Select(m => new ProjectMemorySummary
        {
            Content = m.Content,
            Summary = m.Summary,
            Category = m.Category.ToString(),
            Importance = (int)m.Importance,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task ClearProjectAllMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        // 清除会话记忆
        var sessionMemories = await _storage.GetProjectMemoriesAsync(projectId, cancellationToken);
        foreach (var session in sessionMemories)
        {
            await _storage.DeleteAsync(projectId, session.SessionId, cancellationToken);
        }

        // 清除长期记忆
        await _projectMemoryRepository.DeleteByProjectIdAsync(projectId, cancellationToken);
    }
}
