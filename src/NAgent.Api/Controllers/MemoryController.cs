using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.Memory.Commands;
using NAgent.AgentApplication.Features.Memory.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 记忆管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MemoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public MemoryController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    // ===== 项目长期记忆 =====

    /// <summary>
    /// 获取项目长期记忆摘要
    /// </summary>
    [HttpGet("project/{projectId}/summary")]
    public async Task<ActionResult<ApiResponse<List<ProjectMemorySummaryDto>>>> GetProjectMemorySummary(
        Guid projectId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetProjectMemorySummaryQuery(projectId, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<ProjectMemorySummaryDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 搜索项目长期记忆
    /// </summary>
    [HttpGet("project/{projectId}/search")]
    public async Task<ActionResult<ApiResponse<List<ProjectMemorySummaryDto>>>> SearchProjectMemory(
        Guid projectId, [FromQuery] string query, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var searchQuery = new SearchProjectMemoryQuery(projectId, query, limit);
        var result = await _mediator.Send(searchQuery, cancellationToken);
        return Ok(ApiResponse<List<ProjectMemorySummaryDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 保存项目长期记忆
    /// </summary>
    [HttpPost("project/{projectId}/long-term")]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveProjectLongTermMemory(
        Guid projectId, [FromBody] SaveProjectMemoryRequest request, CancellationToken cancellationToken = default)
    {
        var command = new SaveProjectMemoryCommand(
            projectId,
            request.Content,
            request.Summary,
            request.CategoryId,
            request.Importance,
            request.Metadata);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<Guid>.SuccessResponse(result, "记忆保存成功"));
    }

    /// <summary>
    /// 清除项目所有记忆
    /// </summary>
    [HttpDelete("project/{projectId}")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearProjectMemories(
        Guid projectId, CancellationToken cancellationToken = default)
    {
        var command = new ClearProjectMemoriesCommand(projectId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "项目记忆已清除"));
    }

    // ===== 会话级记忆 =====

    /// <summary>
    /// 获取会话短期记忆
    /// </summary>
    [HttpGet("session/{sessionId}/short-term")]
    public async Task<ActionResult<ApiResponse<List<MemoryEntryDto>>>> GetSessionShortTermMemory(
        Guid sessionId, [FromQuery] Guid projectId, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = new GetSessionShortTermMemoryQuery(projectId, sessionId, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<MemoryEntryDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 获取会话长期记忆
    /// </summary>
    [HttpGet("session/{sessionId}/long-term")]
    public async Task<ActionResult<ApiResponse<List<MemoryEntryDto>>>> GetSessionLongTermMemory(
        Guid sessionId, [FromQuery] Guid projectId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetSessionLongTermMemoryQuery(projectId, sessionId, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<MemoryEntryDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// 清除会话记忆
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearSessionMemory(
        Guid sessionId, [FromQuery] Guid projectId, CancellationToken cancellationToken = default)
    {
        var command = new ClearSessionMemoryCommand(projectId, sessionId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "会话记忆已清除"));
    }
}

/// <summary>
/// 保存项目长期记忆请求
/// </summary>
public record SaveProjectMemoryRequest(
    string Content,
    string Summary,
    int CategoryId,
    int Importance,
    Dictionary<string, object>? Metadata = null);
