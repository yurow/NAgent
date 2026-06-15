using NAgent.AgentDomain.Entities;

namespace NAgent.AgentApplication.Interfaces;

/// <summary>
/// Agent 执行引擎接口 - 抽象不同 Agent Framework 的实现
/// 可通过工厂模式切换 LangChain、Semantic Kernel 等不同实现
/// </summary>
public interface IAgentEngine
{
    /// <summary>
    /// 执行 Agent 任务
    /// </summary>
    Task<AgentExecutionResult> ExecuteAsync(
        AgentSession session, 
        string userInput, 
        string? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析用户意图，选择合适的工具
    /// </summary>
    Task<ToolSelectionResult> ParseIntentAsync(
        AgentSession session, 
        string userInput, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成自然语言回复
    /// </summary>
    Task<string> GenerateResponseAsync(
        AgentSession session, 
        string context, 
        string? modelId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Agent 执行结果
/// </summary>
public record AgentExecutionResult(
    bool Success,
    string Output,
    string? ToolName = null,
    string? ModelName = null,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// 工具选择结果
/// </summary>
public record ToolSelectionResult(
    string ToolName,
    Dictionary<string, object> Parameters,
    string Reasoning);