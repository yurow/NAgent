using System.Text.Json;
using Microsoft.Extensions.Logging;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Agents.LangChain;

/// <summary>
/// 基于 LangChain 的 Agent 引擎实现 - 集成内置工具执行
/// </summary>
public class LangChainAgentEngine : IAgentEngine
{
    private readonly ILlmClient _llmClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<LangChainAgentEngine>? _logger;

    public LangChainAgentEngine(
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        ILogger<LangChainAgentEngine>? logger = null)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentSession session,
        string userInput,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 解析意图
            var intent = await ParseIntentAsync(session, userInput, cancellationToken);

            // 2. 执行工具（如果有）
            if (!string.IsNullOrEmpty(intent.ToolName) && _toolRegistry.HasTool(intent.ToolName))
            {
                _logger?.LogInformation("执行工具: {ToolName}, 项目: {ProjectId}", intent.ToolName, session.ProjectId);

                var tool = _toolRegistry.GetTool(intent.ToolName)!;
                var toolResult = await tool.ExecuteAsync(intent.Parameters, session.ProjectId, cancellationToken);

                if (!toolResult.Success)
                {
                    return new AgentExecutionResult(
                        false,
                        $"工具执行失败: {toolResult.ErrorMessage}",
                        intent.ToolName);
                }

                // 3. 基于工具结果生成最终回复
                var response = await GenerateResponseWithToolResultAsync(
                    session, userInput, tool.ToolName, toolResult.Output, modelId, cancellationToken);

                return new AgentExecutionResult(true, response, intent.ToolName);
            }

            // 3. 直接生成回复
            var context = BuildContext(session, userInput);
            var directResponse = await _llmClient.GenerateAsync(context, modelId, cancellationToken: cancellationToken);

            return new AgentExecutionResult(true, directResponse);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Agent 执行失败");
            return new AgentExecutionResult(false, $"执行失败: {ex.Message}");
        }
    }

    public async Task<ToolSelectionResult> ParseIntentAsync(
        AgentSession session,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildIntentPrompt(session, userInput);

        try
        {
            var response = await _llmClient.GenerateAsync(prompt, cancellationToken: cancellationToken);

            // 尝试解析 JSON 响应
            var json = ExtractJson(response);
            if (!string.IsNullOrEmpty(json))
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var toolName = root.GetProperty("tool").GetString();
                var reasoning = root.GetProperty("reasoning").GetString() ?? "";

                var parameters = new Dictionary<string, object>();
                if (root.TryGetProperty("parameters", out var paramsElement))
                {
                    foreach (var prop in paramsElement.EnumerateObject())
                    {
                        parameters[prop.Name] = prop.Value.ToString()!;
                    }
                }

                return new ToolSelectionResult(toolName ?? "", parameters, reasoning);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "意图解析失败，将直接回复");
        }

        // 默认不调用工具
        return new ToolSelectionResult("", new Dictionary<string, object>(), "直接回复用户");
    }

    public async Task<string> GenerateResponseAsync(
        AgentSession session,
        string context,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildResponsePrompt(session, context);
        return await _llmClient.GenerateAsync(prompt, modelId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 基于工具结果生成回复
    /// </summary>
    private async Task<string> GenerateResponseWithToolResultAsync(
        AgentSession session,
        string userInput,
        string toolName,
        string toolOutput,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"你是 NAgent AI 助手。用户提出了一个问题，你调用了一个工具来获取信息。

用户问题: {userInput}

调用的工具: {toolName}

工具返回结果:
{toolOutput}

请基于工具返回的结果，直接回答用户的问题。如果工具结果不足以回答问题，请说明。
回复:";

        return await _llmClient.GenerateAsync(prompt, modelId, cancellationToken: cancellationToken);
    }

    private string BuildContext(AgentSession session, string currentInput)
    {
        var recentMessages = session.GetRecentMessages(5);
        var history = string.Join("\n", recentMessages.Select(m => $"{m.Role}: {m.Content}"));

        return $@"历史对话:
{history}

当前输入: {currentInput}";
    }

    private string BuildIntentPrompt(AgentSession session, string userInput)
    {
        var context = BuildContext(session, userInput);
        var availableTools = _toolRegistry.GetAllTools();
        var toolsDesc = string.Join("\n", availableTools.Select(t => $"- {t.ToolName}: {t.Description}"));

        return $@"{context}

可用工具:
{toolsDesc}

请分析用户意图。如果需要调用工具，返回以下 JSON 格式（不要包含其他文字）:
{{""tool"": ""工具名"", ""parameters"": {{""参数名"": ""参数值""}}, ""reasoning"": ""推理过程""}}

如果不需要调用工具，返回:
{{""tool"": "", ""parameters"": {{}}, ""reasoning"": ""直接回复的原因""}}";
    }

    private string BuildResponsePrompt(AgentSession session, string context)
    {
        return $@"基于以下上下文，生成友好、专业的回复：

{context}

回复：";
    }

    /// <summary>
    /// 从文本中提取 JSON
    /// </summary>
    private string? ExtractJson(string text)
    {
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return text[startIndex..(endIndex + 1)];
        }

        return null;
    }
}