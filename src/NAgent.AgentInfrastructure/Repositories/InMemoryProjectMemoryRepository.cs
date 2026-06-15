using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// 项目长期记忆内存仓储实现
/// </summary>
public class InMemoryProjectMemoryRepository : IProjectMemoryRepository
{
    private readonly Dictionary<Guid, ProjectMemory> _memories = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public Task<ProjectMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            _memories.TryGetValue(id, out var memory);
            return Task.FromResult(memory);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ProjectMemory>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var memories = _memories.Values
                .Where(m => m.ProjectId == projectId)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectMemory>>(memories);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ProjectMemory>> GetByProjectIdAsync(Guid projectId, MemoryCategory category, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var memories = _memories.Values
                .Where(m => m.ProjectId == projectId && m.Category == category)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectMemory>>(memories);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ProjectMemory>> GetActiveMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var memories = _memories.Values
                .Where(m => m.ProjectId == projectId && !m.IsExpired())
                .OrderByDescending(m => m.CalculateScore())
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectMemory>>(memories);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ProjectMemory>> SearchAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var memories = _memories.Values
                .Where(m => m.ProjectId == projectId && !m.IsExpired())
                .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            m.Summary.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.CalculateScore())
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectMemory>>(memories);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ProjectMemory>> GetTopMemoriesAsync(Guid projectId, int limit = 20, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var memories = _memories.Values
                .Where(m => m.ProjectId == projectId && !m.IsExpired())
                .OrderByDescending(m => m.CalculateScore())
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<ProjectMemory>>(memories);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task AddAsync(ProjectMemory memory, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _memories[memory.Id] = memory;
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateAsync(ProjectMemory memory, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _memories[memory.Id] = memory;
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _memories.Remove(id);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var toRemove = _memories.Values
                .Where(m => m.ProjectId == projectId)
                .Select(m => m.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                _memories.Remove(id);
            }

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<int> GetCountAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var count = _memories.Values.Count(m => m.ProjectId == projectId);
            return Task.FromResult(count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task CleanupExpiredMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var expired = _memories.Values
                .Where(m => m.ProjectId == projectId && m.IsExpired())
                .Select(m => m.Id)
                .ToList();

            foreach (var id in expired)
            {
                _memories.Remove(id);
            }

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
