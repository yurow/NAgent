using MediatR;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 更新模型配置命令处理器
/// </summary>
public class UpdateModelConfigCommandHandler : IRequestHandler<UpdateModelConfigCommand, bool>
{
    private readonly ILlmModelRepository _modelRepository;
    private readonly ILlmProviderRepository _providerRepository;

    public UpdateModelConfigCommandHandler(
        ILlmModelRepository modelRepository,
        ILlmProviderRepository providerRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
    }

    public async Task<bool> Handle(UpdateModelConfigCommand request, CancellationToken cancellationToken)
    {
        var model = await _modelRepository.GetByIdAsync(request.ModelId, cancellationToken);
        
        if (model == null)
        {
            return false;
        }

        // 更新上下文窗口大小
        if (request.ContextWindowSize.HasValue)
        {
            model.UpdateContextWindowSize(request.ContextWindowSize.Value);
        }

        // 更新最大输出 token 数
        if (request.MaxOutputTokens.HasValue)
        {
            model.UpdateMaxOutputTokens(request.MaxOutputTokens.Value);
        }

        // 更新温度参数
        if (request.Temperature.HasValue)
        {
            model.UpdateDefaultTemperature(request.Temperature.Value);
        }

        // 更新启用状态
        if (request.IsEnabled.HasValue)
        {
            model.SetEnabled(request.IsEnabled.Value);
        }

        // 更新默认状态
        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            // 取消所有提供商下其他模型的默认状态（全局唯一默认模型）
            var allModels = await _modelRepository.GetAllEnabledAsync(cancellationToken);
            foreach (var m in allModels.Where(m => m.Id != model.Id && m.IsDefault))
            {
                m.UnsetAsDefault();
                await _modelRepository.UpdateAsync(m, cancellationToken);
            }
            
            model.SetAsDefault();
        }
        else if (request.IsDefault.HasValue && !request.IsDefault.Value)
        {
            model.UnsetAsDefault();
        }

        await _modelRepository.UpdateAsync(model, cancellationToken);
        return true;
    }
}