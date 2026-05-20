using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Queries;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Queries;

/// <summary>
/// 获取模型使用统计查询处理器
/// </summary>
public class GetModelUsageStatsQueryHandler : IRequestHandler<GetModelUsageStatsQuery, ModelUsageStatsDto>
{
    private readonly ILlmModelRepository _modelRepository;
    private readonly ILlmModelDailyUsageRepository _dailyUsageRepository;

    public GetModelUsageStatsQueryHandler(
        ILlmModelRepository modelRepository,
        ILlmModelDailyUsageRepository dailyUsageRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _dailyUsageRepository = dailyUsageRepository ?? throw new ArgumentNullException(nameof(dailyUsageRepository));
    }

    public async Task<ModelUsageStatsDto> Handle(GetModelUsageStatsQuery request, CancellationToken cancellationToken)
    {
        var model = await _modelRepository.GetByIdAsync(request.ModelId, cancellationToken);
        
        if (model == null)
        {
            throw new InvalidOperationException($"未找到 ID 为 {request.ModelId} 的模型");
        }

        // 获取近3天的使用统计
        var dailyUsage = await _dailyUsageRepository.GetRecentDaysAsync(request.ModelId, 3, cancellationToken);
        
        var dailyUsageDtos = dailyUsage.Select(u => new ModelDailyUsageDto(
            u.UsageDate,
            u.TotalTokens,
            u.RequestCount
        )).OrderBy(u => u.UsageDate).ToList();

        return new ModelUsageStatsDto(
            model.Id,
            model.TotalTokenUsage,
            dailyUsageDtos
        );
    }
}
