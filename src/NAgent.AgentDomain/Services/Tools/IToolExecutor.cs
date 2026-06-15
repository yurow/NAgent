namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 工具执行器接口 - 所有内置工具的统一执行契约
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="parameters">工具参数</param>
    /// <param name="projectId">当前项目ID（用于路径隔离）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工具执行结果</returns>
    Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 执行输出
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 附加元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public static ToolExecutionResult Ok(string output, Dictionary<string, object>? metadata = null)
    {
        return new ToolExecutionResult { Success = true, Output = output, Metadata = metadata };
    }

    public static ToolExecutionResult Fail(string errorMessage)
    {
        return new ToolExecutionResult { Success = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
/// 工具注册表 - 管理所有可用工具
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// 注册工具
    /// </summary>
    void Register(IToolExecutor tool);

    /// <summary>
    /// 获取工具
    /// </summary>
    IToolExecutor? GetTool(string name);

    /// <summary>
    /// 获取所有工具
    /// </summary>
    IReadOnlyList<IToolExecutor> GetAllTools();

    /// <summary>
    /// 判断工具是否存在
    /// </summary>
    bool HasTool(string name);
}
