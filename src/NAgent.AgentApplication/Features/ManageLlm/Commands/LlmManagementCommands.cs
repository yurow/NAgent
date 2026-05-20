using MediatR;
using NAgent.AgentApplication.DTOs;

namespace NAgent.AgentApplication.Features.ManageLlm.Commands;

/// <summary>
/// 添加 LLM 提供商命令
/// </summary>
public record AddLlmProviderCommand(
    string Name,
    NAgent.AgentDomain.Enums.LlmProtocolType ProtocolType,
    string BaseUrl,
    string ApiKey) : IRequest<LlmProviderDto>;

/// <summary>
/// 添加 LLM 模型命令
/// </summary>
public record AddLlmModelCommand(
    Guid ProviderId,
    string ModelId,
    string DisplayName,
    int ContextWindowSize,
    int MaxOutputTokens = 2048,
    double DefaultTemperature = 0.7,
    bool IsEnabled = true) : IRequest<LlmModelDto>;

/// <summary>
/// 更新模型配置命令
/// </summary>
public record UpdateModelConfigCommand(
    Guid ModelId,
    int? ContextWindowSize = null,
    int? MaxOutputTokens = null,
    double? Temperature = null,
    bool? IsDefault = null,
    bool? IsEnabled = null) : IRequest<bool>;

/// <summary>
/// 删除模型命令
/// </summary>
public record DeleteModelCommand(Guid ModelId) : IRequest<bool>;

/// <summary>
/// 切换当前使用模型命令
/// </summary>
public record SwitchModelCommand(string ModelId) : IRequest<bool>;

/// <summary>
/// 更新 LLM 提供商命令
/// </summary>
public record UpdateLlmProviderCommand(
    Guid ProviderId,
    string? Name = null,
    string? BaseUrl = null,
    string? ApiKey = null,
    bool? IsEnabled = null,
    NAgent.AgentDomain.Enums.LlmProtocolType? ProtocolType = null) : IRequest<bool>;

/// <summary>
/// 删除 LLM 提供商命令
/// </summary>
public record DeleteLlmProviderCommand(Guid ProviderId) : IRequest<bool>;