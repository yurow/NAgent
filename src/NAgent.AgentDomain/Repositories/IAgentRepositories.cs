using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// Agent 会话仓储接口
/// </summary>
public interface IAgentSessionRepository
{
    Task<AgentSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentSession?> GetBySessionKeyAsync(string sessionKey, CancellationToken cancellationToken = default);
    Task AddAsync(AgentSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Agent 工具仓储接口
/// </summary>
public interface IAgentToolRepository
{
    Task<AgentTool?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentTool?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<AgentTool>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AgentTool tool, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentTool tool, CancellationToken cancellationToken = default);
}

/// <summary>
/// LLM 提供商仓储接口
/// </summary>
public interface ILlmProviderRepository
{
    Task<LlmProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LlmProvider>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<LlmProvider?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(LlmProvider provider, CancellationToken cancellationToken = default);
    Task UpdateAsync(LlmProvider provider, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// LLM 模型仓储接口
/// </summary>
public interface ILlmModelRepository
{
    Task<LlmModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LlmModel?> GetByModelIdAsync(string modelId, Guid providerId, CancellationToken cancellationToken = default);
    Task<List<LlmModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<LlmModel?> GetDefaultModelAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<List<LlmModel>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(LlmModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(LlmModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// LLM 模型每日使用统计仓储接口
/// </summary>
public interface ILlmModelDailyUsageRepository
{
    Task<LlmModelDailyUsage?> GetByDateAsync(Guid modelId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<LlmModelDailyUsage>> GetRecentDaysAsync(Guid modelId, int days, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(LlmModelDailyUsage usage, CancellationToken cancellationToken = default);
}