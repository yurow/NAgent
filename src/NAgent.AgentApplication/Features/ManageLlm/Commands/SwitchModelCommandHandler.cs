using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 切换模型命令处理器
/// </summary>
public class SwitchModelCommandHandler : IRequestHandler<SwitchModelCommand, bool>
{
    private readonly ILlmClient _llmClient;
    private readonly ILlmModelRepository _modelRepository;

    public SwitchModelCommandHandler(
        ILlmClient llmClient,
        ILlmModelRepository modelRepository)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    public async Task<bool> Handle(SwitchModelCommand request, CancellationToken cancellationToken)
    {
        // 验证模型是否存在且启用
        var models = await _modelRepository.GetAllEnabledAsync(cancellationToken);
        var model = models.FirstOrDefault(m => m.ModelId == request.ModelId);

        if (model == null)
        {
            throw new InvalidOperationException($"模型 {request.ModelId} 不存在或已禁用");
        }

        // 切换当前使用的模型
        _llmClient.SetCurrentModel(request.ModelId);

        return true;
    }
}
