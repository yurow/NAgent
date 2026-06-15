using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// Tool 定义内存仓储实现
/// </summary>
public class InMemoryToolDefinitionRepository : IToolDefinitionRepository
{
    private readonly Dictionary<Guid, ToolDefinition> _tools = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public Task<ToolDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            _tools.TryGetValue(id, out var tool);
            return Task.FromResult(tool);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ToolDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var tool = _tools.Values.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(tool);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ToolDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            return Task.FromResult<IReadOnlyList<ToolDefinition>>(_tools.Values.ToList());
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ToolDefinition>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var tools = _tools.Values
                .Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult<IReadOnlyList<ToolDefinition>>(tools);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ToolDefinition>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var tools = _tools.Values
                .Where(t => t.IsEnabled)
                .ToList();
            return Task.FromResult<IReadOnlyList<ToolDefinition>>(tools);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<ToolDefinition>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            // 这里简化实现，实际应该通过 Skill 关联查询
            var tools = _tools.Values
                .Where(t => t.IsEnabled)
                .ToList();
            return Task.FromResult<IReadOnlyList<ToolDefinition>>(tools);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task AddAsync(ToolDefinition tool, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _tools[tool.Id] = tool;
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateAsync(ToolDefinition tool, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _tools[tool.Id] = tool;
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
            _tools.Remove(id);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var exists = _tools.Values.Any(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
