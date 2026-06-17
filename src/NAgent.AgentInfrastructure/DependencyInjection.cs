using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentInfrastructure.Agents;
using NAgent.AgentInfrastructure.Agents.LangChain;
using NAgent.AgentInfrastructure.Security;
using NAgent.AgentInfrastructure.Llm;
using NAgent.AgentInfrastructure.Sandbox;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services;
using NAgent.AgentDomain.Services.Memory;
using NAgent.AgentDomain.Services.Skills;
using NAgent.AgentDomain.Services.Tools;
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
            var config = sp.GetRequiredService<IConfiguration>();
            var workspacePath = config["Workspace:BasePath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "workspace");
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

        // ⭐ 注册 Skill 执行器（编排 Tool 调用）
        services.AddScoped<ISkillExecutor, SkillExecutor>();

        // ⭐ 注册内置工具系统（通过 YAML 配置 + YamlToolExecutor）
        RegisterBuiltInTools(services);

        // ⭐ 注册意图分类与推测服务
        services.AddScoped<IIntentService, IntentServiceImpl>();

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

        // 注册自动加载服务
        services.AddSingleton<SkillsAndToolsAutoLoader>();

        // 注册初始化服务（启动时自动加载）
        var skillsDir = configuration["Skills:Directory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skills");
        var toolsDir = configuration["Tools:Directory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");

        // 确保目录存在
        if (!Directory.Exists(skillsDir))
            Directory.CreateDirectory(skillsDir);
        if (!Directory.Exists(toolsDir))
            Directory.CreateDirectory(toolsDir);
    }

    /// <summary>
    /// 注册内置工具（Web搜索、文件读取、文件写入、文件列表）
    /// 工具通过 YAML 配置 + YamlToolExecutor 执行
    /// </summary>
    private static void RegisterBuiltInTools(IServiceCollection services)
    {
        // 注册工具注册表 - 从 YAML 文件加载工具定义并创建执行器
        services.AddSingleton<IToolRegistry>(sp =>
        {
            var registry = new ToolRegistry();
            var workspaceManager = sp.GetRequiredService<IWorkspaceManager>();
            var toolDefRepo = sp.GetRequiredService<IToolDefinitionRepository>();
            var logger = sp.GetService<ILogger<ToolRegistry>>();

            // 1. 从 IToolDefinitionRepository 加载 YAML 配置工具
            var yamlTools = toolDefRepo.GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            foreach (var toolDef in yamlTools.Where(t => t.IsEnabled))
            {
                try
                {
                    registry.RegisterFromDefinition(toolDef, workspaceManager, logger);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "加载 YAML 工具 {ToolName} 失败", toolDef.Name);
                }
            }

            // 2. 如果 YAML 中没有定义内置工具，则注册硬编码的内置工具作为后备
            var builtInToolNames = new[] { "web_search", "local_file_read", "local_file_write", "list_workspace_files" };
            foreach (var toolName in builtInToolNames)
            {
                if (!registry.HasTool(toolName))
                {
                    // 创建简化的内置工具（通过 local 执行类型映射到内置逻辑）
                    var builtInDef = new NAgent.AgentDomain.Entities.ToolDefinition(
                        toolName,
                        GetBuiltInToolDescription(toolName),
                        "built-in",
                        $"name: {toolName}\ndescription: {GetBuiltInToolDescription(toolName)}\ncategory: built-in\nexecution:\n  type: local",
                        $"built-in/{toolName}.yaml"
                    );
                    registry.RegisterFromDefinition(builtInDef, workspaceManager, logger);
                }
            }

            return registry;
        });
    }

    private static string GetBuiltInToolDescription(string toolName)
    {
        return toolName.ToLowerInvariant() switch
        {
            "web_search" => "使用多搜索引擎（百度+Bing）进行网络搜索，获取实时信息",
            "local_file_read" => "读取项目工作空间内的文件内容",
            "local_file_write" => "在项目工作空间内创建或修改文件",
            "list_workspace_files" => "遍历并返回项目工作目录下所有文件名称和路径",
            _ => "内置工具"
        };
    }
}
