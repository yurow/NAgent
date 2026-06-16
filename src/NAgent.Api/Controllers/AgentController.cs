using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.ExecuteAgent.Commands;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;
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
    private readonly IIntentService _intentService;
    private readonly IToolRegistry _toolRegistry;

    public AgentController(IMediator mediator, ILlmClient llmClient, IWorkspaceManager workspaceManager, IIntentService intentService, IToolRegistry toolRegistry)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _intentService = intentService ?? throw new ArgumentNullException(nameof(intentService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
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
        var absolutePath = _workspaceManager.EnsureProjectWorkspace(userId, projectId);

        // ⭐ 检测并预执行工具调用（如文件读取）
        var toolResults = await PreExecuteToolsAsync(request.UserInput, projectId, cancellationToken);
        var toolResultsStr = toolResults.Count > 0
            ? "\n\n" + string.Join("\n\n", toolResults.Select(tr => $"[工具执行结果: {tr.ToolName}]\n{tr.Output}"))
            : "";

        var contextPrompt = $@"你是 NAgent AI 助手。你在一个项目的工作目录中协助用户。

当前工作目录(相对路径): {relativePath}
当前工作目录(绝对路径): {absolutePath}

工作目录文件列表:
{string.Join("\n", files)}
{toolResultsStr}

项目规范文档 (spec.md):
{specContent}

用户问题: {request.UserInput}

请基于工作目录上下文回答用户问题。如果用户要求读取文件，请根据已执行的工具结果直接展示文件内容。如果用户要求总结文档，请基于文件内容进行总结。如果用户要求创建或修改文件，请说明将如何使用文件工具。";

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

            // ⭐ 保存对话历史
            SaveConversationToHistory(userId, projectId, request.SessionId, request.UserInput, fullResponse.ToString());

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
    /// 保存对话到历史记录（追加模式）
    /// </summary>
    private void SaveConversationToHistory(Guid userId, Guid projectId, string sessionKey, string userInput, string assistantReply)
    {
        try
        {
            // 加载现有历史
            var existingMessages = _workspaceManager.LoadChatHistory(userId, projectId, sessionKey, 1000);

            // 追加新消息
            existingMessages.Add(new ChatMessageDto
            {
                Role = "User",
                Content = userInput,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });
            existingMessages.Add(new ChatMessageDto
            {
                Role = "Assistant",
                Content = assistantReply,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            });

            // 保持最多 100 条消息
            var trimmed = existingMessages.Count > 100
                ? existingMessages.TakeLast(100).ToList()
                : existingMessages;

            _workspaceManager.SaveChatHistory(userId, projectId, sessionKey, trimmed);
        }
        catch
        {
            // 保存失败不影响主流程
        }
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public IActionResult GetHistory(string sessionId, [FromQuery] string? projectId = null, [FromQuery] int count = 10)
    {
        if (string.IsNullOrEmpty(projectId) || !Guid.TryParse(projectId, out var projectIdGuid))
            return BadRequest(ApiResponse.FailureResponse("缺少 projectId 参数"));

        // 从 JWT Token 获取当前用户ID
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResponse("无法获取用户信息"));

        var messages = _workspaceManager.LoadChatHistory(userId, projectIdGuid, sessionId, count);

        return Ok(ApiResponse<List<ChatMessageDto>>.SuccessResponse(messages));
    }

    /// <summary>
    /// 清除会话记忆
    /// </summary>
    [HttpDelete("memory/{sessionId}")]
    public IActionResult ClearMemory(string sessionId, [FromQuery] string? projectId = null)
    {
        if (string.IsNullOrEmpty(projectId) || !Guid.TryParse(projectId, out var projectIdGuid))
            return BadRequest(ApiResponse.FailureResponse("缺少 projectId 参数"));

        // 从 JWT Token 获取当前用户ID
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResponse("无法获取用户信息"));

        // 清空对话历史（保存空列表）
        _workspaceManager.SaveChatHistory(userId, projectIdGuid, sessionId, new List<ChatMessageDto>());

        return Ok(ApiResponse.SuccessResponse("会话记忆已清除"));
    }

    /// <summary>
    /// 轻量意图分类：仅根据用户最新输入返回固定意图枚举标识
    /// </summary>
    [HttpPost("classify-intent")]
    public async Task<IActionResult> ClassifyIntentAsync([FromBody] ClassifyIntentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
            return BadRequest(ApiResponse.FailureResponse("用户输入不能为空"));

        // 构建对话摘要（仅前 1-2 轮简短摘要）
        var summaries = request.RecentSummaries ?? new List<ChatSummary>();

        var result = await _intentService.ClassifyIntentAsync(
            request.UserInput,
            summaries,
            request.ModelId,
            cancellationToken);

        return Ok(ApiResponse<IntentClassificationResult>.SuccessResponse(result));
    }

    /// <summary>
    /// 深度意图推测：结合短期记忆 + 知识库 + Skills/Tools 推测用户真实意图
    /// </summary>
    [HttpPost("infer-intent")]
    public async Task<IActionResult> InferIntentAsync([FromBody] InferIntentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
            return BadRequest(ApiResponse.FailureResponse("用户输入不能为空"));

        if (string.IsNullOrWhiteSpace(request.ProjectId) || !Guid.TryParse(request.ProjectId, out var projectId))
            return BadRequest(ApiResponse.FailureResponse("缺少有效的 projectId"));

        // 从 JWT Token 获取当前用户ID
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResponse("无法获取用户信息"));

        // 获取短期对话记忆（最近 3 轮 = 6 条消息）
        var shortTermMessages = _workspaceManager.LoadChatHistory(userId, projectId, request.SessionId ?? "", 6);

        var result = await _intentService.InferIntentAsync(
            request.UserInput,
            projectId,
            request.SessionId ?? "",
            shortTermMessages,
            request.ModelId,
            cancellationToken);

        return Ok(ApiResponse<IntentInferenceResult>.SuccessResponse(result));
    }

    /// <summary>
    /// 检测并预执行工具调用（文件读取、文件列表等）
    /// 从用户输入中提取文件名并自动执行对应工具
    /// </summary>
    private async Task<List<ToolPreResult>> PreExecuteToolsAsync(
        string userInput, Guid projectId, CancellationToken cancellationToken)
    {
        var results = new List<ToolPreResult>();

        try
        {
            var inputLower = userInput.ToLowerInvariant();

            // 检测文件读取意图：提取文件名
            var fileReadKeywords = new[] { "读取", "查看", "显示", "打开", "看看", "看一下", "内容", "read", "show", "display", "open" };
            var isFileRead = fileReadKeywords.Any(kw => inputLower.Contains(kw));

            if (isFileRead && _toolRegistry.HasTool("local_file_read"))
            {
                // 提取文件名：匹配常见文件扩展名
                var filePatterns = new[]
                {
                    @"[\w\-\/\\]+\.\w{1,10}",  // 匹配 xxx.ext 格式
                };

                var extractedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pattern in filePatterns)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(userInput, pattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var fileName = match.Value.Trim();
                        // 跳过明显的非文件名
                        if (fileName.Contains("http") || fileName.Length > 100) continue;
                        extractedFiles.Add(fileName);
                    }
                }

                // 也检测引号内的文件名
                var quotedMatches = System.Text.RegularExpressions.Regex.Matches(userInput, """['""`']([^'""`']+)['""`']""");
                foreach (System.Text.RegularExpressions.Match match in quotedMatches)
                {
                    var fileName = match.Groups[1].Value.Trim();
                    if (fileName.Contains('.') && fileName.Length < 100)
                        extractedFiles.Add(fileName);
                }

                // 执行文件读取工具
                var readTool = _toolRegistry.GetTool("local_file_read");
                if (readTool != null)
                {
                    foreach (var filePath in extractedFiles.Take(3)) // 最多读取 3 个文件
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["file_path"] = filePath
                        };

                        var result = await readTool.ExecuteAsync(parameters, projectId, cancellationToken);
                        if (result.Success)
                        {
                            results.Add(new ToolPreResult("local_file_read", $"文件: {filePath}\n{result.Output}"));
                        }
                        else
                        {
                            results.Add(new ToolPreResult("local_file_read", $"文件: {filePath}\n读取失败: {result.ErrorMessage}"));
                        }
                    }
                }
            }

            // 检测文件列表意图
            var listKeywords = new[] { "文件列表", "目录结构", "所有文件", "有哪些文件", "list files", "directory" };
            var isFileList = listKeywords.Any(kw => inputLower.Contains(kw));

            if (isFileList && _toolRegistry.HasTool("list_workspace_files"))
            {
                var listTool = _toolRegistry.GetTool("list_workspace_files");
                if (listTool != null)
                {
                    var result = await listTool.ExecuteAsync(new Dictionary<string, object>(), projectId, cancellationToken);
                    if (result.Success)
                    {
                        results.Add(new ToolPreResult("list_workspace_files", result.Output));
                    }
                }
            }
        }
        catch
        {
            // 工具预执行失败不影响主流程
        }

        return results;
    }
}

/// <summary>
/// 工具预执行结果
/// </summary>
internal record ToolPreResult(string ToolName, string Output);

/// <summary>
/// Agent 执行请求 DTO
/// </summary>
public record ExecuteAgentRequest(string SessionId, string UserInput, string ProjectId, string? ModelId = null);

/// <summary>
/// 意图分类请求
/// </summary>
public record ClassifyIntentRequest(
    string UserInput,
    List<ChatSummary>? RecentSummaries = null,
    string? ModelId = null);

/// <summary>
/// 意图推测请求
/// </summary>
public record InferIntentRequest(
    string UserInput,
    string ProjectId,
    string? SessionId = null,
    string? ModelId = null);
