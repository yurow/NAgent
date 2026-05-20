using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 删除 LLM 提供商命令处理器
/// </summary>
public class DeleteLlmProviderCommandHandler : IRequestHandler<DeleteLlmProviderCommand, bool>
{
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelRepository _modelRepository;

    public DeleteLlmProviderCommandHandler(
        ILlmProviderRepository providerRepository,
        ILlmModelRepository modelRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    public async Task<bool> Handle(DeleteLlmProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(request.ProviderId, cancellationToken);
        
        if (provider == null)
        {
            return false;
        }

        // 先删除该提供商下的所有模型
        var models = await _modelRepository.GetByProviderIdAsync(request.ProviderId, cancellationToken);
        foreach (var model in models)
        {
            await _modelRepository.DeleteAsync(model.Id, cancellationToken);
        }

        // 删除提供商
        await _providerRepository.DeleteAsync(request.ProviderId, cancellationToken);
        return true;
    }
}
