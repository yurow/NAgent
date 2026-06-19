using SqlSugar;
using Microsoft.Extensions.Caching.Memory;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// 角色仓储实现（SQLite 持久化 + 内存缓存）
/// </summary>
public class SqliteAgentRoleRepository : IAgentRoleRepository
{
    private readonly ISqlSugarClient _db;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<Guid, AgentRole> _localCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _cacheInitialized = false;

    public SqliteAgentRoleRepository(ISqlSugarClient db, IMemoryCache cache)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _db.CodeFirst.InitTables<AgentRole>();
    }

    private async Task EnsureCacheInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheInitialized)
            return;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cacheInitialized)
                return;

            var roles = await _db.Queryable<AgentRole>().ToListAsync();
            _localCache.Clear();
            foreach (var role in roles)
            {
                _localCache[role.Id] = role;
            }
            _cacheInitialized = true;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<AgentRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);

        if (_localCache.TryGetValue(id, out var role))
            return role;

        role = await _db.Queryable<AgentRole>().InSingleAsync(id);
        if (role != null)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _localCache[id] = role;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        return role;
    }

    public async Task<List<AgentRole>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAt)
            .ToList();
    }

    public async Task AddAsync(AgentRole role, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(role).ExecuteCommandAsync();

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[role.Id] = role;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task UpdateAsync(AgentRole role, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(role).ExecuteCommandAsync();

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[role.Id] = role;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<AgentRole>().In(id).ExecuteCommandAsync();

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache.Remove(id);
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
