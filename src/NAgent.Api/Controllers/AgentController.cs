using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.ExecuteAgent.Commands;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// AI Agent 控制器 - 使用 CQRS 模式
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("AI Agent")]
[Authorize] // ⭐ 所有 Agent API 都需要认证
public class AgentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 执行 Agent 任务
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteAsync([FromBody] ExecuteAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(ApiResponse.FailureResponse("会话ID不能为空"));
        }

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest(ApiResponse.FailureResponse("用户输入不能为空"));
        }

        var command = new ExecuteAgentCommand(request.SessionId, request.UserInput);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.Success)
        {
            return Ok(ApiResponse.SuccessResponse(result.Output));
        }
        else
        {
            return BadRequest(ApiResponse.FailureResponse(result.Output));
        }
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public IActionResult GetHistory(string sessionId, [FromQuery] int count = 10)
    {
        // TODO: 添加 GetSessionHistoryQuery
        return Ok(ApiResponse.SuccessResponse("暂无历史"));
    }

    /// <summary>
    /// 清除会话记忆
    /// </summary>
    [HttpDelete("memory/{sessionId}")]
    public IActionResult ClearMemory(string sessionId)
    {
        // TODO: 添加 ClearSessionMemoryCommand
        return Ok(ApiResponse.SuccessResponse("会话记忆已清除"));
    }
}

/// <summary>
/// Agent 执行请求 DTO
/// </summary>
public record ExecuteAgentRequest(string SessionId, string UserInput);