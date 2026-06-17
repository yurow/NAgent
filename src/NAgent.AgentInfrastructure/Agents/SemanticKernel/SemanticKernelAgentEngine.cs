using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;

namespace NAgent.AgentInfrastructure.Agents.SemanticKernel;

/// <summary>
/// 基于 Microsoft Semantic Kernel 的 Agent 引擎实现
/// 预留实现，展示如何通过工厂模式切换不同的 Agent Framework
/// </summary>
public class SemanticKernelAgentEngine : IAgentEngine
{
    private readonly ILlmClient _llmClient;

    public SemanticKernelAgentEngine(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        // TODO: 初始化 Semantic Kernel
    }

    public Task<AgentExecutionResult> ExecuteAsync(
        AgentSession session, 
        string userInput, 
        string? modelId = null,
        CancellationToken cancellationToken = default,
        Action<DebugEvent>? onDebugEvent = null)
    {
        // TODO: 使用 Semantic Kernel 实现 Agent 执行逻辑
        throw new NotImplementedException("Semantic Kernel 实现待完成");
    }

    public Task<ToolSelectionResult> ParseIntentAsync(
        AgentSession session, 
        string userInput, 
        CancellationToken cancellationToken = default)
    {
        // TODO: 使用 Semantic Kernel 解析意图
        throw new NotImplementedException("Semantic Kernel 实现待完成");
    }

    public async Task<string> GenerateResponseAsync(
        AgentSession session, 
        string context, 
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        // 可以使用相同的 LLM 客户端
        return await _llmClient.GenerateAsync(context, modelId, cancellationToken: cancellationToken);
    }
}