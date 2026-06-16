using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.ExecuteAgent.Commands;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Repositories;
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
    private readonly IWorkspaceManager _workspaceManager;

    public AgentController(IMediator mediator, ILlmClient llmClient, IWorkspaceManager workspaceManager)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
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

        // 从 JWT Token 获取当前用户ID
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<string>.FailureResponse("无法获取用户信息"));
        }

        var command = new ExecuteAgentCommand(request.SessionId, request.UserInput, request.ProjectId, userId, request.ModelId);
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

        // 从 JWT Token 获取当前用户ID
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Response.StatusCode = 401;
            await Response.WriteAsJsonAsync(ApiResponse<string>.FailureResponse("无法获取用户信息"), cancellationToken);
            return;
        }

        var projectId = Guid.Parse(request.ProjectId);

        // 1. 确保工作目录存在
        var workspacePath = _workspaceManager.EnsureProjectWorkspace(userId, projectId);
        var relativePath = _workspaceManager.GetProjectRelativePath(userId, projectId);

        // 2. 检查是否已初始化（未初始化时走 execute 端点的初始化逻辑）
        var isInitialized = _workspaceManager.IsInitialized(userId, projectId);
        if (!isInitialized)
        {
            // 未初始化：调用非流式 execute 进行初始化，然后返回
            var command = new ExecuteAgentCommand(request.SessionId, request.UserInput, request.ProjectId, userId, request.ModelId);
            var result = await _mediator.Send(command, cancellationToken);

            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            if (result.Success)
            {
                // 将初始化回复分段输出模拟流式效果
                var chunks = SplitIntoChunks(result.Output ?? "", 50);
                foreach (var chunk in chunks)
                {
                    await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    await Task.Delay(30, cancellationToken); // 模拟打字效果
                }
            }
            else
            {
                await Response.WriteAsync($"data: ERROR: {result.Output}\n\n", cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            return;
        }

        // 3. 已初始化：构建带工作目录上下文的 prompt
        var specContent = _workspaceManager.ReadSpecFile(userId, projectId);
        var files = _workspaceManager.GetWorkspaceFiles(userId, projectId);

        var contextPrompt = $@"你是 NAgent AI 助手。你在一个项目的工作目录中协助用户。

当前工作目录(相对路径): {relativePath}

工作目录文件列表:
{string.Join("\n", files)}

项目规范文档 (spec.md):
{specContent}

用户问题: {request.UserInput}

请基于工作目录上下文回答用户问题。如果用户要求创建或修改文件，请使用文件工具。";

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var fullResponse = new System.Text.StringBuilder();

            await foreach (var chunk in _llmClient.GenerateStreamAsync(
                contextPrompt,
                request.ModelId,
                cancellationToken: cancellationToken))
            {
                fullResponse.Append(chunk);
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            sw.Stop();

            // 记录 LLM 调用
            _workspaceManager.RecordLlmCall(userId, projectId, "chat_stream", request.ModelId ?? "default", contextPrompt, fullResponse.ToString(), sw.ElapsedMilliseconds);

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 记录错误
            _workspaceManager.RecordLlmCall(userId, projectId, "chat_stream_error", request.ModelId ?? "default", contextPrompt, "", 0, ex.Message);
            await Response.WriteAsync($"data: ERROR: {ex.Message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 将文本分割成小块模拟流式输出
    /// </summary>
    private List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks;
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
