using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// LLM 提供商仓储实现（内存存储）
/// </summary>
public class InMemoryLlmProviderRepository : ILlmProviderRepository
{
    private readonly Dictionary<Guid, LlmProvider> _providers = new();

    public Task<LlmProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _providers.TryGetValue(id, out var provider);
        return Task.FromResult(provider);
    }

    public Task<List<LlmProvider>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var enabledProviders = _providers.Values.Where(p => p.IsEnabled).ToList();
        return Task.FromResult(enabledProviders);
    }

    public Task<LlmProvider?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var provider = _providers.Values.FirstOrDefault(p => p.Name == name);
        return Task.FromResult(provider);
    }

    public Task AddAsync(LlmProvider provider, CancellationToken cancellationToken = default)
    {
        _providers[provider.Id] = provider;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LlmProvider provider, CancellationToken cancellationToken = default)
    {
        _providers[provider.Id] = provider;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _providers.Remove(id);
        return Task.CompletedTask;
    }

    public Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}