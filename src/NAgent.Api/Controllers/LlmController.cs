using MediatR;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.DTOs;
using NAgent.AgentApplication.Features.ManageLlm.Commands;
using NAgent.AgentApplication.Features.ManageLlm.Queries;
using NAgent.AgentApplication.Interfaces;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// LLM 模型管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("LLM Management")]
public class LlmController : ControllerBase
{
    private readonly IMediator _mediator;

    public LlmController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 添加 LLM 提供商（支持 OpenAI 和 Anthropic 协议）
    /// </summary>
    [HttpPost("providers")]
    public async Task<IActionResult> AddProvider([FromBody] AddLlmProviderRequest request, CancellationToken cancellationToken)
    {
        var command = new AddLlmProviderCommand(
            request.Name,
            request.ProtocolType,
            request.BaseUrl,
            request.ApiKey
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<LlmProviderDto>.SuccessResponse(result));
    }

    /// <summary>
    /// 获取所有 LLM 提供商
    /// </summary>
    [HttpGet("providers")]
    public async Task<IActionResult> GetAllProviders(CancellationToken cancellationToken)
    {
        var query = new GetAllLlmProvidersQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<LlmProviderDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 为指定提供商添加模型
    /// </summary>
    [HttpPost("providers/{providerId}/models")]
    public async Task<IActionResult> AddModel(Guid providerId, [FromBody] AddLlmModelRequest request, CancellationToken cancellationToken)
    {
        var command = new AddLlmModelCommand(
            providerId,
            request.ModelId,
            request.DisplayName,
            request.ContextWindowSize,
            request.MaxOutputTokens,
            request.DefaultTemperature,
            request.IsEnabled
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<LlmModelDto>.SuccessResponse(result));
    }

    /// <summary>
    /// 获取所有可用模型列表
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetAllModels(CancellationToken cancellationToken)
    {
        var query = new GetAllAvailableModelsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<AvailableModel>>.SuccessResponse(result));
    }

    /// <summary>
    /// 切换当前使用的模型
    /// </summary>
    [HttpPost("models/switch")]
    public async Task<IActionResult> SwitchModel([FromBody] SwitchModelRequest request, CancellationToken cancellationToken)
    {
        var command = new SwitchModelCommand(request.ModelId);
        await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("模型切换成功"));
    }

    /// <summary>
    /// 获取当前使用的模型
    /// </summary>
    [HttpGet("models/current")]
    public async Task<IActionResult> GetCurrentModel(CancellationToken cancellationToken)
    {
        var query = new GetCurrentModelQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { CurrentModel = result }));
    }

    /// <summary>
    /// 更新模型配置（上下文窗口、最大输出等）
    /// </summary>
    [HttpPut("models/{modelId}")]
    public async Task<IActionResult> UpdateModelConfig(Guid modelId, [FromBody] UpdateModelConfigRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateModelConfigCommand(
            modelId,
            request.ContextWindowSize,
            request.MaxOutputTokens,
            request.Temperature,
            request.IsDefault,
            request.IsEnabled
        );

        var result = await _mediator.Send(command, cancellationToken);
        return result 
            ? Ok(ApiResponse.SuccessResponse("模型配置更新成功"))
            : BadRequest(ApiResponse.FailureResponse("模型配置更新失败"));
    }

    /// <summary>
    /// 删除模型
    /// </summary>
    [HttpDelete("models/{modelId}")]
    public async Task<IActionResult> DeleteModel(Guid modelId, CancellationToken cancellationToken)
    {
        var command = new DeleteModelCommand(modelId);
        var result = await _mediator.Send(command, cancellationToken);
        return result 
            ? Ok(ApiResponse.SuccessResponse("模型删除成功"))
            : NotFound(ApiResponse.FailureResponse("模型不存在"));
    }

    /// <summary>
    /// 获取模型使用统计
    /// </summary>
    [HttpGet("models/{modelId}/usage")]
    public async Task<IActionResult> GetModelUsageStats(Guid modelId, CancellationToken cancellationToken)
    {
        var query = new GetModelUsageStatsQuery(modelId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<ModelUsageStatsDto>.SuccessResponse(result));
    }

    /// <summary>
    /// 更新 LLM 提供商
    /// </summary>
    [HttpPut("providers/{providerId}")]
    public async Task<IActionResult> UpdateProvider(Guid providerId, [FromBody] UpdateLlmProviderRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateLlmProviderCommand(
            providerId,
            request.Name,
            request.BaseUrl,
            request.ApiKey,
            request.IsEnabled,
            request.ProtocolType
        );

        var result = await _mediator.Send(command, cancellationToken);
        return result 
            ? Ok(ApiResponse.SuccessResponse("提供商更新成功"))
            : NotFound(ApiResponse.FailureResponse("提供商不存在"));
    }

    /// <summary>
    /// 删除 LLM 提供商
    /// </summary>
    [HttpDelete("providers/{providerId}")]
    public async Task<IActionResult> DeleteProvider(Guid providerId, CancellationToken cancellationToken)
    {
        var command = new DeleteLlmProviderCommand(providerId);
        var result = await _mediator.Send(command, cancellationToken);
        return result 
            ? Ok(ApiResponse.SuccessResponse("提供商删除成功"))
            : NotFound(ApiResponse.FailureResponse("提供商不存在"));
    }

    /// <summary>
    /// 修复协议类型为0的提供商（临时修复）
    /// </summary>
    [HttpPost("providers/fix-protocol")]
    public async Task<IActionResult> FixInvalidProtocolTypes(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAllLlmProvidersQuery();
            var providers = await _mediator.Send(query, cancellationToken);
            
            int fixedCount = 0;
            foreach (var provider in providers)
            {
                if (provider.ProtocolType == 0)
                {
                    var command = new UpdateLlmProviderCommand(
                        provider.Id,
                        provider.Name,
                        provider.BaseUrl,
                        null, // 不修改 API Key
                        provider.IsEnabled,
                        NAgent.AgentDomain.Enums.LlmProtocolType.OpenAI // 修复为 OpenAI 协议
                    );
                    
                    await _mediator.Send(command, cancellationToken);
                    fixedCount++;
                }
            }
            
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                FixedCount = fixedCount,
                Message = fixedCount > 0 
                    ? $"已修复 {fixedCount} 个协议类型为0的提供商，已设置为OpenAI协议" 
                    : "没有发现协议类型为0的提供商"
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.FailureResponse($"修复失败: {ex.Message}"));
        }
    }
}

/// <summary>
/// 切换模型请求 DTO
/// </summary>
public record SwitchModelRequest(string ModelId);

/// <summary>
/// 更新 LLM 提供商请求 DTO
/// </summary>
public record UpdateLlmProviderRequest(
    string? Name = null,
    string? BaseUrl = null,
    string? ApiKey = null,
    bool? IsEnabled = null,
    NAgent.AgentDomain.Enums.LlmProtocolType? ProtocolType = null);