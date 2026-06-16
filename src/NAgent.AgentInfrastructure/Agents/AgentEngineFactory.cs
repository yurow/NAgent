using Microsoft.Extensions.DependencyInjection;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Services.Skills;
using NAgent.AgentDomain.Services.Tools;
using NAgent.AgentInfrastructure.Agents.LangChain;
using NAgent.AgentInfrastructure.Agents.SemanticKernel;

namespace NAgent.AgentInfrastructure.Agents;

/// <summary>
/// Agent 引擎类型枚举
/// </summary>
public enum AgentEngineType
{
    LangChain,
    SemanticKernel
}

/// <summary>
/// Agent 引擎工厂 - 支持切换不同的 Agent Framework
/// </summary>
public class AgentEngineFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AgentEngineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 创建指定类型的 Agent 引擎
    /// </summary>
    public IAgentEngine Create(AgentEngineType type, IServiceProvider? serviceProvider = null)
    {
        var sp = serviceProvider ?? _serviceProvider;
        return type switch
        {
            AgentEngineType.LangChain => CreateLangChainEngine(sp),
            AgentEngineType.SemanticKernel => CreateSemanticKernelEngine(sp),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"不支持的 Agent 引擎类型: {type}")
        };
    }

    private IAgentEngine CreateLangChainEngine(IServiceProvider serviceProvider)
    {
        var llmClient = serviceProvider.GetRequiredService<ILlmClient>();
        var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();
        var skillExecutor = serviceProvider.GetRequiredService<ISkillExecutor>();
        var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<LangChainAgentEngine>>();
        return new LangChainAgentEngine(llmClient, toolRegistry, skillExecutor, logger);
    }

    private IAgentEngine CreateSemanticKernelEngine(IServiceProvider serviceProvider)
    {
        var llmClient = serviceProvider.GetRequiredService<ILlmClient>();
        return new SemanticKernelAgentEngine(llmClient);
    }
}