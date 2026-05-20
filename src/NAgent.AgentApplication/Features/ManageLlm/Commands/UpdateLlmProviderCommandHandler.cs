using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 更新 LLM 提供商命令处理器
/// </summary>
public class UpdateLlmProviderCommandHandler : IRequestHandler<UpdateLlmProviderCommand, bool>
{
    private readonly ILlmProviderRepository _providerRepository;

    public UpdateLlmProviderCommandHandler(ILlmProviderRepository providerRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
    }

    public async Task<bool> Handle(UpdateLlmProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(request.ProviderId, cancellationToken);
        
        if (provider == null)
        {
            return false;
        }

        // 更新名称
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            provider.UpdateName(request.Name);
        }

        // 更新 BaseUrl
        if (!string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            provider.UpdateBaseUrl(request.BaseUrl);
        }

        // 更新 ApiKey
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            provider.UpdateApiKey(request.ApiKey);
        }

        // 更新启用状态
        if (request.IsEnabled.HasValue)
        {
            provider.SetEnabled(request.IsEnabled.Value);
        }

        // 更新协议类型
        if (request.ProtocolType.HasValue)
        {
            provider.UpdateProtocolType(request.ProtocolType.Value);
        }

        await _providerRepository.UpdateAsync(provider, cancellationToken);
        return true;
    }
}