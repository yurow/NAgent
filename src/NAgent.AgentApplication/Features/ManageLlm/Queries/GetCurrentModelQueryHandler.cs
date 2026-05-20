using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Queries;
using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentApplication.Features.ManageLlm.Queries;

/// <summary>
/// 获取当前使用模型查询处理器
/// </summary>
public class GetCurrentModelQueryHandler : IRequestHandler<GetCurrentModelQuery, string>
{
    private readonly ILlmClient _llmClient;

    public GetCurrentModelQueryHandler(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    public Task<string> Handle(GetCurrentModelQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_llmClient.GetCurrentModel());
    }
}
