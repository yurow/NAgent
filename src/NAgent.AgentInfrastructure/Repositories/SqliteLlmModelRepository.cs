using SqlSugar;
using Microsoft.Extensions.Caching.Memory;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// LLM 模型仓储实现（SQLite 持久化 + 内存缓存）
/// </summary>
public class SqliteLlmModelRepository : ILlmModelRepository
{
    private readonly ISqlSugarClient _db;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<Guid, LlmModel> _localCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _cacheInitialized = false;

    public SqliteLlmModelRepository(ISqlSugarClient db, IMemoryCache cache)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _db.CodeFirst.InitTables<LlmModel>();
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

            var models = await _db.Queryable<LlmModel>().ToListAsync();
            _localCache.Clear();
            foreach (var model in models)
            {
                _localCache[model.Id] = model;
            }
            _cacheInitialized = true;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<LlmModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        
        if (_localCache.TryGetValue(id, out var model))
            return model;

        model = await _db.Queryable<LlmModel>().InSingleAsync(id);
        if (model != null)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _localCache[id] = model;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        return model;
    }

    public async Task<LlmModel?> GetByModelIdAsync(string modelId, Guid providerId, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values.FirstOrDefault(m => m.ModelId == modelId && m.ProviderId == providerId);
    }

    public async Task<List<LlmModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values.Where(m => m.ProviderId == providerId).ToList();
    }

    public async Task<LlmModel?> GetDefaultModelAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values.FirstOrDefault(m => m.ProviderId == providerId && m.IsDefault);
    }

    public async Task<List<LlmModel>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values.Where(m => m.IsEnabled).ToList();
    }

    public async Task AddAsync(LlmModel model, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(model).ExecuteCommandAsync();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[model.Id] = model;
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task UpdateAsync(LlmModel model, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(model).ExecuteCommandAsync();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[model.Id] = model;
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<LlmModel>().In(id).ExecuteCommandAsync();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache.Remove(id);
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache.Clear();
            var models = await _db.Queryable<LlmModel>().ToListAsync();
            foreach (var model in models)
            {
                _localCache[model.Id] = model;
            }
            _cacheInitialized = true;
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private void InvalidateCache()
    {
        _cache.Remove("AllEnabledProviders");
    }
}