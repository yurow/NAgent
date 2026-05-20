using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentInfrastructure.Agents;
using NAgent.AgentInfrastructure.Agents.LangChain;
using NAgent.AgentInfrastructure.Security;
using NAgent.AgentInfrastructure.Llm;
using NAgent.AgentInfrastructure.Sandbox;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentInfrastructure.Repositories;
using NAgent.AgentInfrastructure.Services;
using SqlSugar;

namespace NAgent.AgentInfrastructure;

/// <summary>
/// Agent Infrastructure 依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAgentInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 注册 Agent Engine（默认使用 LangChain）
        var engineType = configuration.GetValue("Agent:EngineType", "LangChain");
        
        services.AddScoped<IAgentEngine>(sp =>
        {
            var factory = sp.GetRequiredService<AgentEngineFactory>();
            var type = Enum.Parse<AgentEngineType>(engineType, true);
            return factory.Create(type, sp);
        });

        // 注册工厂
        services.AddSingleton<AgentEngineFactory>();

        // ⭐ 注册 Agent 领域仓储（SQLite 持久化 + 内存缓存）
        services.AddSingleton<IAgentSessionRepository, InMemoryAgentSessionRepository>();
        services.AddSingleton<IAgentToolRepository, InMemoryAgentToolRepository>();
        
        // ⭐ 注册 LLM 仓储（SQLite 持久化 + 内存缓存）
        services.AddScoped<ILlmProviderRepository, SqliteLlmProviderRepository>();
        services.AddScoped<ILlmModelRepository, SqliteLlmModelRepository>();
        services.AddSingleton<ILlmModelDailyUsageRepository, InMemoryLlmModelDailyUsageRepository>();

        // 注册多模型 LLM 客户端
        services.AddScoped<ILlmClient, MultiModelLlmClient>();

        // 注册沙箱执行器
        var sandboxEndpoint = configuration["Agent:SandboxEndpoint"] ?? "http://localhost:8080";
        services.AddSingleton<ISandboxExecutor>(sp => new CubeSandboxExecutorImpl(sandboxEndpoint));

        // 注册安全组件
        services.AddSingleton<IPromptFilter, PromptFilterImpl>();
        services.AddSingleton<ISandboxResultValidator, SandboxResultValidatorImpl>();

        return services;
    }
}