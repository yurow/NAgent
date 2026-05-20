using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentApplication.Features.ManageLlm.Queries;

/// <summary>
/// 获取所有 LLM 提供商查询
/// </summary>
public record GetAllLlmProvidersQuery : IRequest<List<LlmProviderDto>>;

/// <summary>
/// 获取指定提供商的模型列表查询
/// </summary>
public record GetModelsByProviderQuery(Guid ProviderId) : IRequest<List<LlmModelDto>>;

/// <summary>
/// 获取所有可用模型查询
/// </summary>
public record GetAllAvailableModelsQuery : IRequest<List<AvailableModel>>;

/// <summary>
/// 获取当前使用的模型查询
/// </summary>
public record GetCurrentModelQuery : IRequest<string>;

/// <summary>
/// 获取模型使用统计查询
/// </summary>
public record GetModelUsageStatsQuery(Guid ModelId) : IRequest<ModelUsageStatsDto>;

/// <summary>
/// 模型使用统计 DTO
/// </summary>
public record ModelUsageStatsDto(
    Guid ModelId,
    long TotalTokenUsage,
    List<ModelDailyUsageDto> DailyUsage);
