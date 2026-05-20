using NAgent.AgentCore.Security;

namespace NAgent.AgentCore.Agent;

/// <summary>
/// 工具分发器 - 根据安全等级分发到本地或沙箱执行
/// </summary>
public class ToolDispatcher
{
    private readonly Dictionary<string, ITool> _localTools = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ISandboxTool> _sandboxTools = new(StringComparer.OrdinalIgnoreCase);

    public ToolDispatcher()
    {
        // 初始化工具注册表
        InitializeTools();
    }

    /// <summary>
    /// 分派工具执行
    /// </summary>
    public async Task<ToolExecutionResult> DispatchAsync(ToolIntent intent, CancellationToken cancellationToken = default)
    {
        var securityLevel = ToolLevelConfig.GetSecurityLevel(intent.ToolName);

        return securityLevel switch
        {
            ToolLevelConfig.SecurityLevel.Low => await ExecuteLocalToolAsync(intent, cancellationToken),
            ToolLevelConfig.SecurityLevel.Medium => await ExecuteMediumRiskToolAsync(intent, cancellationToken),
            ToolLevelConfig.SecurityLevel.High => await ExecuteSandboxToolAsync(intent, cancellationToken),
            _ => throw new InvalidOperationException($"未知的安全等级: {securityLevel}")
        };
    }

    private async Task<ToolExecutionResult> ExecuteLocalToolAsync(ToolIntent intent, CancellationToken cancellationToken)
    {
        if (!_localTools.TryGetValue(intent.ToolName, out var tool))
        {
            return ToolExecutionResult.Error($"未找到本地工具: {intent.ToolName}");
        }

        try
        {
            var result = await tool.ExecuteAsync(intent.Parameters, cancellationToken);
            return ToolExecutionResult.Success(result, usedSandbox: false);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"本地工具执行失败: {ex.Message}");
        }
    }

    private async Task<ToolExecutionResult> ExecuteMediumRiskToolAsync(ToolIntent intent, CancellationToken cancellationToken)
    {
        // TODO: 中危工具需要额外验证逻辑
        // 例如：用户确认、权限检查等
        return await ExecuteLocalToolAsync(intent, cancellationToken);
    }

    private async Task<ToolExecutionResult> ExecuteSandboxToolAsync(ToolIntent intent, CancellationToken cancellationToken)
    {
        if (!_sandboxTools.TryGetValue(intent.ToolName, out var sandboxTool))
        {
            return ToolExecutionResult.Error($"未找到沙箱工具: {intent.ToolName}");
        }

        try
        {
            var result = await sandboxTool.ExecuteInSandboxAsync(intent.Parameters, cancellationToken);
            return ToolExecutionResult.Success(result, usedSandbox: true);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"沙箱工具执行失败: {ex.Message}");
        }
    }

    private void InitializeTools()
    {
        // TODO: 从依赖注入容器或配置中加载工具
        // 这里只是示例结构
    }

    /// <summary>
    /// 注册本地工具
    /// </summary>
    public void RegisterLocalTool(string name, ITool tool)
    {
        _localTools[name] = tool;
    }

    /// <summary>
    /// 注册沙箱工具
    /// </summary>
    public void RegisterSandboxTool(string name, ISandboxTool tool)
    {
        _sandboxTools[name] = tool;
    }
}

/// <summary>
/// 工具意图
/// </summary>
public record ToolIntent(string ToolName, Dictionary<string, object> Parameters);

/// <summary>
/// 工具执行结果
/// </summary>
public record ToolExecutionResult(bool IsSuccess, string Output, bool UsedSandbox, Dictionary<string, object>? Metadata = null)
{
    public static ToolExecutionResult Success(string output, bool usedSandbox = false, Dictionary<string, object>? metadata = null)
        => new(true, output, usedSandbox, metadata);

    public static ToolExecutionResult Error(string message)
        => new(false, message, false);
}

/// <summary>
/// 本地工具接口
/// </summary>
public interface ITool
{
    Task<string> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// 沙箱工具接口
/// </summary>
public interface ISandboxTool
{
    Task<string> ExecuteInSandboxAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
