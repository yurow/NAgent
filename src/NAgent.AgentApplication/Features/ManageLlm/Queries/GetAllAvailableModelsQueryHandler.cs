using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Queries;
using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentApplication.Features.ManageLlm.Queries;

/// <summary>
/// 获取所有可用模型查询处理器
/// </summary>
public class GetAllAvailableModelsQueryHandler : IRequestHandler<GetAllAvailableModelsQuery, List<AvailableModel>>
{
    private readonly ILlmClient _llmClient;

    public GetAllAvailableModelsQueryHandler(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    public async Task<List<AvailableModel>> Handle(GetAllAvailableModelsQuery request, CancellationToken cancellationToken)
    {
        return await _llmClient.GetAvailableModelsAsync(cancellationToken);
    }
}
