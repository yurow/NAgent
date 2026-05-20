using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 添加 LLM 模型命令处理器
/// </summary>
public class AddLlmModelCommandHandler : IRequestHandler<AddLlmModelCommand, LlmModelDto>
{
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelRepository _modelRepository;

    public AddLlmModelCommandHandler(
        ILlmProviderRepository providerRepository,
        ILlmModelRepository modelRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    public async Task<LlmModelDto> Handle(AddLlmModelCommand request, CancellationToken cancellationToken)
    {
        // 获取提供商
        var provider = await _providerRepository.GetByIdAsync(request.ProviderId, cancellationToken);
        
        if (provider == null)
        {
            throw new InvalidOperationException($"未找到 ID 为 {request.ProviderId} 的提供商");
        }

        // 创建模型
        var model = new LlmModel(
            request.ModelId,
            request.DisplayName,
            request.ContextWindowSize,
            request.MaxOutputTokens,
            request.DefaultTemperature,
            provider.Id
        );

        // 设置启用状态
        if (!request.IsEnabled)
        {
            model.SetEnabled(false);
        }

        // 添加到提供商
        provider.AddModel(model);
        await _modelRepository.AddAsync(model, cancellationToken);
        await _providerRepository.UpdateAsync(provider, cancellationToken);

        return MapToDto(model);
    }

    private static LlmModelDto MapToDto(LlmModel model)
    {
        return new LlmModelDto(
            model.Id,
            model.ModelId,
            model.DisplayName,
            model.ContextWindowSize,
            model.MaxOutputTokens,
            model.DefaultTemperature,
            model.IsDefault,
            model.IsEnabled,
            model.ProviderId,
            model.TotalTokenUsage,
            model.CreatedAt
        );
    }
}