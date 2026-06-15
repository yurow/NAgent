using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentInfrastructure.Agents;
using NAgent.AgentInfrastructure.Agents.LangChain;
using NAgent.AgentInfrastructure.Security;
using NAgent.AgentInfrastructure.Llm;
using NAgent.AgentInfrastructure.Sandbox;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services;
using NAgent.AgentDomain.Services.Memory;
using NAgent.AgentInfrastructure.Repositories;
using NAgent.AgentInfrastructure.Services;
using NAgent.AgentInfrastructure.Parsers;
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

        // ⭐ 注册 LLM 模型缓存服务（领域服务层）
        services.AddScoped<LlmModelCacheService>();

        // ⭐ 注册记忆系统（项目级隔离）
        services.AddSingleton<IMemoryStorage>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var workspacePath = configuration["Workspace:BasePath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NAgent", "workspace");
            return new FileMemoryStorage(workspacePath);
        });

        // ⭐ 注册项目长期记忆仓储
        services.AddSingleton<IProjectMemoryRepository, InMemoryProjectMemoryRepository>();

        services.AddSingleton<IMemorySystemFactory, MemorySystemFactory>();
        services.AddScoped<IMemorySystem>(sp =>
        {
            var factory = sp.GetRequiredService<IMemorySystemFactory>();
            return factory.CreateMemorySystem();
        });

        // 注册多模型 LLM 客户端
        services.AddScoped<ILlmClient, MultiModelLlmClient>();

        // 注册沙箱执行器
        var sandboxEndpoint = configuration["Agent:SandboxEndpoint"] ?? "http://localhost:8080";
        services.AddSingleton<ISandboxExecutor>(sp => new CubeSandboxExecutorImpl(sandboxEndpoint));

        // 注册安全组件
        services.AddSingleton<IPromptFilter, PromptFilterImpl>();
        services.AddSingleton<ISandboxResultValidator, SandboxResultValidatorImpl>();

        // ⭐ 注册 Skills 和 Tools 系统
        RegisterSkillsAndTools(services, configuration);

        return services;
    }

    /// <summary>
    /// 注册 Skills 和 Tools 系统
    /// </summary>
    private static void RegisterSkillsAndTools(IServiceCollection services, IConfiguration configuration)
    {
        // 注册解析器
        services.AddSingleton<ISkillParser, SkillMarkdownParser>();
        services.AddSingleton<IToolDefinitionParser, ToolYamlParser>();

        // 注册加载器
        services.AddSingleton<ISkillLoader, SkillFileLoader>();
        services.AddSingleton<IToolLoader, ToolFileLoader>();

        // 注册仓储
        services.AddSingleton<ISkillRepository, InMemorySkillRepository>();
        services.AddSingleton<IToolDefinitionRepository, InMemoryToolDefinitionRepository>();

        // 注册初始化服务（启动时自动加载）
        var skillsDir = configuration["Skills:Directory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skills");
        var toolsDir = configuration["Tools:Directory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");

        // 确保目录存在
        if (!Directory.Exists(skillsDir))
            Directory.CreateDirectory(skillsDir);
        if (!Directory.Exists(toolsDir))
            Directory.CreateDirectory(toolsDir);
    }
}
