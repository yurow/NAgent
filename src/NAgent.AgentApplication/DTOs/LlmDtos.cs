using NAgent.AgentDomain.Enums;

namespace NAgent.AgentApplication.DTOs;

/// <summary>
/// LLM 提供商 DTO
/// </summary>
public record LlmProviderDto(
    Guid Id,
    string Name,
    LlmProtocolType ProtocolType,
    string BaseUrl,
    bool IsEnabled,
    List<LlmModelDto> Models,
    DateTime CreatedAt);

/// <summary>
/// LLM 模型 DTO
/// </summary>
public record LlmModelDto(
    Guid Id,
    string ModelId,
    string DisplayName,
    int ContextWindowSize,
    int MaxOutputTokens,
    double DefaultTemperature,
    bool IsDefault,
    bool IsEnabled,
    Guid ProviderId,
    long TotalTokenUsage,
    DateTime CreatedAt);

/// <summary>
/// 模型每日使用统计 DTO
/// </summary>
public record ModelDailyUsageDto(
    DateTime UsageDate,
    long TotalTokens,
    int RequestCount);

/// <summary>
/// 添加 LLM 提供商命令 DTO
/// </summary>
public record AddLlmProviderRequest(
    string Name,
    LlmProtocolType ProtocolType,
    string BaseUrl,
    string ApiKey);

/// <summary>
/// 添加 LLM 模型命令 DTO
/// </summary>
public record AddLlmModelRequest(
    Guid ProviderId,
    string ModelId,
    string DisplayName,
    int ContextWindowSize,
    int MaxOutputTokens = 2048,
    double DefaultTemperature = 0.7,
    bool IsEnabled = true);

/// <summary>
/// 更新模型配置请求 DTO
/// </summary>
public record UpdateModelConfigRequest(
    int? ContextWindowSize = null,
    int? MaxOutputTokens = null,
    double? Temperature = null,
    bool? IsDefault = null,
    bool? IsEnabled = null);