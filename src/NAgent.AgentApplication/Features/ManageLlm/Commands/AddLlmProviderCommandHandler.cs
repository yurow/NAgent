using MediatR;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 添加 LLM 提供商命令处理器
/// </summary>
public class AddLlmProviderCommandHandler : IRequestHandler<AddLlmProviderCommand, LlmProviderDto>
{
    private readonly ILlmProviderRepository _providerRepository;

    public AddLlmProviderCommandHandler(ILlmProviderRepository providerRepository)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
    }

    public async Task<LlmProviderDto> Handle(AddLlmProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = new LlmProvider(
            request.Name,
            request.ProtocolType,
            request.BaseUrl,
            request.ApiKey
        );

        await _providerRepository.AddAsync(provider, cancellationToken);

        return MapToDto(provider);
    }

    private static LlmProviderDto MapToDto(LlmProvider provider)
    {
        return new LlmProviderDto(
            provider.Id,
            provider.Name,
            provider.ProtocolType,
            provider.BaseUrl,
            provider.IsEnabled,
            provider.Models.Select(MapModelToDto).ToList(),
            provider.CreatedAt
        );
    }

    private static LlmModelDto MapModelToDto(LlmModel model)
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