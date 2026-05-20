using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// LLM 模型仓储实现（内存存储）
/// </summary>
public class InMemoryLlmModelRepository : ILlmModelRepository
{
    private readonly Dictionary<Guid, LlmModel> _models = new();

    public Task<LlmModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _models.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task<LlmModel?> GetByModelIdAsync(string modelId, Guid providerId, CancellationToken cancellationToken = default)
    {
        var model = _models.Values.FirstOrDefault(m => m.ModelId == modelId && m.ProviderId == providerId);
        return Task.FromResult(model);
    }

    public Task<List<LlmModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var models = _models.Values.Where(m => m.ProviderId == providerId).ToList();
        return Task.FromResult(models);
    }

    public Task<LlmModel?> GetDefaultModelAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var defaultModel = _models.Values.FirstOrDefault(m => m.ProviderId == providerId && m.IsDefault);
        return Task.FromResult(defaultModel);
    }

    public Task<List<LlmModel>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var enabledModels = _models.Values.Where(m => m.IsEnabled).ToList();
        return Task.FromResult(enabledModels);
    }

    public Task AddAsync(LlmModel model, CancellationToken cancellationToken = default)
    {
        _models[model.Id] = model;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LlmModel model, CancellationToken cancellationToken = default)
    {
        _models[model.Id] = model;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _models.Remove(id);
        return Task.CompletedTask;
    }

    public Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}