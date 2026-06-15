using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// Skill 内存仓储实现
/// </summary>
public class InMemorySkillRepository : ISkillRepository
{
    private readonly Dictionary<Guid, Skill> _skills = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            _skills.TryGetValue(id, out var skill);
            return Task.FromResult(skill);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var skill = _skills.Values.FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(skill);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<Skill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            return Task.FromResult<IReadOnlyList<Skill>>(_skills.Values.ToList());
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<Skill>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var skills = _skills.Values
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult<IReadOnlyList<Skill>>(skills);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<Skill>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var skills = _skills.Values
                .Where(s => s.IsEnabled)
                .ToList();
            return Task.FromResult<IReadOnlyList<Skill>>(skills);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _skills[skill.Id] = skill;
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _skills[skill.Id] = skill;
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
            _skills.Remove(id);
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
            var exists = _skills.Values.Any(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
