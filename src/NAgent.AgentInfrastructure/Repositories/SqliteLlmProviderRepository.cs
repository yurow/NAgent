using SqlSugar;
using Microsoft.Extensions.Caching.Memory;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// LLM 提供商仓储实现（SQLite 持久化 + 内存缓存）
/// </summary>
public class SqliteLlmProviderRepository : ILlmProviderRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ILlmModelRepository _modelRepository;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<Guid, LlmProvider> _localCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _cacheInitialized = false;

    private const string AllProvidersCacheKey = "AllProviders";

    public SqliteLlmProviderRepository(
        ISqlSugarClient db, 
        ILlmModelRepository modelRepository,
        IMemoryCache cache)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _db.CodeFirst.InitTables<LlmProvider>();
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

            var providers = await _db.Queryable<LlmProvider>().ToListAsync();
            _localCache.Clear();
            foreach (var provider in providers)
            {
                _localCache[provider.Id] = provider;
            }
            _cacheInitialized = true;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<LlmProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        
        if (_localCache.TryGetValue(id, out var provider))
            return provider;

        provider = await _db.Queryable<LlmProvider>().InSingleAsync(id);
        if (provider != null)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _localCache[id] = provider;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        return provider;
    }

    public async Task<List<LlmProvider>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        
        var providers = _localCache.Values.Where(p => p.IsEnabled).ToList();
        
        // 加载每个提供商的模型
        foreach (var provider in providers)
        {
            var models = await _modelRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
            provider.Models.Clear();
            provider.Models.AddRange(models);
        }
        
        return providers;
    }

    public async Task<LlmProvider?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureCacheInitializedAsync(cancellationToken);
        return _localCache.Values.FirstOrDefault(p => p.Name == name);
    }

    public async Task AddAsync(LlmProvider provider, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(provider).ExecuteCommandAsync();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[provider.Id] = provider;
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task UpdateAsync(LlmProvider provider, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(provider).ExecuteCommandAsync();
        
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _localCache[provider.Id] = provider;
            InvalidateCache();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<LlmProvider>().In(id).ExecuteCommandAsync();
        
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
            var providers = await _db.Queryable<LlmProvider>().ToListAsync();
            foreach (var provider in providers)
            {
                _localCache[provider.Id] = provider;
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