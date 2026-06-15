using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.ExecuteAgent.Commands;
using NAgent.AgentApplication.Interfaces;
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
    private readonly ILlmClient _llmClient;

    public AgentController(IMediator mediator, ILlmClient llmClient)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    /// <summary>
    /// 执行 Agent 任务
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteAsync([FromBody] ExecuteAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(ApiResponse<string>.FailureResponse("会话ID不能为空"));
        }

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest(ApiResponse<string>.FailureResponse("用户输入不能为空"));
        }

        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            return BadRequest(ApiResponse<string>.FailureResponse("项目ID不能为空"));
        }

        var command = new ExecuteAgentCommand(request.SessionId, request.UserInput, request.ProjectId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.Success)
        {
            return Ok(ApiResponse<string>.SuccessResponse(result.Output));
        }
        else
        {
            return BadRequest(ApiResponse<string>.FailureResponse(result.Output));
        }
    }

    /// <summary>
    /// 执行 Agent 任务（流式输出）
    /// </summary>
    [HttpPost("execute-stream")]
    public async Task ExecuteStreamAsync([FromBody] ExecuteAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(ApiResponse<string>.FailureResponse("会话ID不能为空"), cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(ApiResponse<string>.FailureResponse("用户输入不能为空"), cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(ApiResponse<string>.FailureResponse("项目ID不能为空"), cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var chunk in _llmClient.GenerateStreamAsync(
                request.UserInput,
                cancellationToken: cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await Response.WriteAsync($"data: ERROR: {ex.Message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
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
public record ExecuteAgentRequest(string SessionId, string UserInput, string ProjectId, string? ModelId = null);