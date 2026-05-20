using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// LLM 模型每日使用统计仓储实现（内存存储）
/// </summary>
public class InMemoryLlmModelDailyUsageRepository : ILlmModelDailyUsageRepository
{
    private readonly Dictionary<string, LlmModelDailyUsage> _usages = new();

    public Task<LlmModelDailyUsage?> GetByDateAsync(Guid modelId, DateTime date, CancellationToken cancellationToken = default)
    {
        var key = $"{modelId}_{date.Date:yyyy-MM-dd}";
        _usages.TryGetValue(key, out var usage);
        return Task.FromResult(usage);
    }

    public async Task<List<LlmModelDailyUsage>> GetRecentDaysAsync(Guid modelId, int days, CancellationToken cancellationToken = default)
    {
        var result = new List<LlmModelDailyUsage>();
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var usage = await GetByDateAsync(modelId, date, cancellationToken);
            if (usage != null)
            {
                result.Add(usage);
            }
        }

        return result;
    }

    public Task AddOrUpdateAsync(LlmModelDailyUsage usage, CancellationToken cancellationToken = default)
    {
        var key = $"{usage.ModelId}_{usage.UsageDate:yyyy-MM-dd}";
        _usages[key] = usage;
        return Task.CompletedTask;
    }
}
