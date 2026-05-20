using Microsoft.Extensions.DependencyInjection;

namespace NAgent.AgentCore;

/// <summary>
/// AgentCore 依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAgentCore(this IServiceCollection services, Action<AgentOptions>? configure = null)
    {
        var options = new AgentOptions();
        configure?.Invoke(options);

        // 注册安全组件
        services.AddSingleton<Security.PromptFilter>();
        services.AddSingleton<Security.SandboxResultCheck>();

        // 注册 Agent 核心组件
        services.AddSingleton<Agent.MemoryManager>();
        services.AddSingleton<Agent.ToolDispatcher>();
        services.AddSingleton<Agent.AgentRunner>();

        // 注册 LLM 客户端
        services.AddSingleton(sp => new LLm.LocalLlmClient(options.ModelPath));

        // 注册沙箱客户端
        services.AddSingleton(sp => new Sandbox.CubeSandboxClient(options.SandboxEndpoint));

        // 注册示例工具
        services.AddSingleton<Tools.LocalTools.CalculatorTool>();
        services.AddSingleton<Tools.SandboxTools.CodeExecutorTool>();

        return services;
    }
}

/// <summary>
/// Agent 配置选项
/// </summary>
public class AgentOptions
{
    /// <summary>
    /// 本地模型路径（GGUF 格式）
    /// </summary>
    public string ModelPath { get; set; } = "./models/llama-2-7b.Q4_K_M.gguf";

    /// <summary>
    /// 沙箱服务端点
    /// </summary>
    public string SandboxEndpoint { get; set; } = "http://localhost:8080";

    /// <summary>
    /// 最大会话数
    /// </summary>
    public int MaxSessions { get; set; } = 1000;

    /// <summary>
    /// 请求超时秒数
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 60;
}
