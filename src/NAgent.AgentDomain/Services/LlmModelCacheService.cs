using Microsoft.Extensions.Caching.Memory;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentDomain.Services;

/// <summary>
/// LLM 模型缓存服务 - 领域服务层，负责模型和提供商的缓存管理
/// </summary>
public class LlmModelCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelRepository _modelRepository;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string AllEnabledProvidersKey = "AllEnabledProviders";
    private const string CurrentModelKey = "CurrentModel";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public LlmModelCacheService(
        IMemoryCache cache,
        ILlmProviderRepository providerRepository,
        ILlmModelRepository modelRepository)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    /// <summary>
    /// 获取所有启用的提供商及其模型（带缓存）
    /// </summary>
    public async Task<List<LlmProvider>> GetAllEnabledProvidersAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<List<LlmProvider>>(AllEnabledProvidersKey, out var cachedProviders) && cachedProviders != null)
        {
            return cachedProviders;
        }

        var providers = await _providerRepository.GetAllEnabledAsync(cancellationToken);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheExpiration)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (reason == EvictionReason.Expired || reason == EvictionReason.Removed)
                {
                }
            });

        _cache.Set(AllEnabledProvidersKey, providers, cacheEntryOptions);
        return providers;
    }

    /// <summary>
    /// 设置当前选中的模型
    /// </summary>
    public async Task SetCurrentModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var result = await ResolveModelAsync(modelId, cancellationToken);
        
        if (!result.HasValue)
        {
            throw new InvalidOperationException($"模型 {modelId} 不存在或已禁用");
        }

        var (provider, model) = result.Value;
        var cacheEntry = new ModelCacheEntry(provider, model);
        _cache.Set(CurrentModelKey, cacheEntry, CacheExpiration);
    }

    /// <summary>
    /// 获取当前选中的模型（带缓存）
    /// </summary>
    public async Task<(LlmProvider Provider, LlmModel Model)?> GetCurrentModelAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<ModelCacheEntry>(CurrentModelKey, out var cachedEntry) && cachedEntry != null)
        {
            return (cachedEntry.Provider, cachedEntry.Model);
        }

        return null;
    }

    /// <summary>
    /// 解析模型配置（带缓存）
    /// </summary>
    public async Task<(LlmProvider Provider, LlmModel Model)?> ResolveModelAsync(
        string? modelId, 
        CancellationToken cancellationToken = default)
    {
        var providers = await GetAllEnabledProvidersAsync(cancellationToken);

        if (!providers.Any())
        {
            return null;
        }

        LlmModel? model = null;
        LlmProvider? provider = null;

        if (!string.IsNullOrEmpty(modelId))
        {
            foreach (var p in providers)
            {
                var foundModel = p.Models.FirstOrDefault(m => m.ModelId == modelId && m.IsEnabled);
                if (foundModel != null)
                {
                    model = foundModel;
                    provider = p;
                    break;
                }
            }
        }

        if (model == null)
        {
            foreach (var p in providers)
            {
                var defaultModel = p.Models.FirstOrDefault(m => m.IsDefault && m.IsEnabled);
                if (defaultModel != null)
                {
                    model = defaultModel;
                    provider = p;
                    break;
                }
            }
        }

        if (model == null && providers.Any())
        {
            provider = providers.First();
            model = provider.Models.FirstOrDefault(m => m.IsEnabled);
        }

        if (model == null || provider == null)
        {
            return null;
        }

        return (provider, model);
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(AllEnabledProvidersKey);
        _cache.Remove(CurrentModelKey);
    }

    /// <summary>
    /// 刷新缓存
    /// </summary>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            ClearCache();
            await GetAllEnabledProvidersAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 模型缓存条目
    /// </summary>
    private record ModelCacheEntry(LlmProvider Provider, LlmModel Model);
}