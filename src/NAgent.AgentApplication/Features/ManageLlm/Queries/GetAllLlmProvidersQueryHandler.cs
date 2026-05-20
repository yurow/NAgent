using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Queries;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Queries;

/// <summary>
/// 获取所有 LLM 提供商查询处理器
/// </summary>
public class GetAllLlmProvidersQueryHandler : IRequestHandler<GetAllLlmProvidersQuery, List<LlmProviderDto>>
{
    private readonly ILlmProviderRepository _providerRepository;

    public GetAllLlmProvidersQueryHandler(ILlmProviderRepository providerRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
    }

    public async Task<List<LlmProviderDto>> Handle(GetAllLlmProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await _providerRepository.GetAllEnabledAsync(cancellationToken);
        
        return providers.Select(p => new LlmProviderDto(
            p.Id,
            p.Name,
            p.ProtocolType,
            p.BaseUrl,
            p.IsEnabled,
            p.Models.Select(m => new LlmModelDto(
                m.Id,
                m.ModelId,
                m.DisplayName,
                m.ContextWindowSize,
                m.MaxOutputTokens,
                m.DefaultTemperature,
                m.IsDefault,
                m.IsEnabled,
                m.ProviderId,
                m.TotalTokenUsage,
                m.CreatedAt
            )).ToList(),
            p.CreatedAt
        )).ToList();
    }
}
