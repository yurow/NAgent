using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 删除模型命令处理器
/// </summary>
public class DeleteModelCommandHandler : IRequestHandler<DeleteModelCommand, bool>
{
    private readonly ILlmModelRepository _modelRepository;
    private readonly ILlmProviderRepository _providerRepository;

    public DeleteModelCommandHandler(
        ILlmModelRepository modelRepository,
        ILlmProviderRepository providerRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
    }

    public async Task<bool> Handle(DeleteModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _modelRepository.GetByIdAsync(request.ModelId, cancellationToken);
        
        if (model == null)
        {
            return false;
        }

        // 从提供商中移除模型
        var provider = await _providerRepository.GetByIdAsync(model.ProviderId, cancellationToken);
        if (provider != null)
        {
            provider.RemoveModel(model.ModelId);
            await _providerRepository.UpdateAsync(provider, cancellationToken);
        }

        // 删除模型记录
        await _modelRepository.DeleteAsync(request.ModelId, cancellationToken);
        return true;
    }
}
