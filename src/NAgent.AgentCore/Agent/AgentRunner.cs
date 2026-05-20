using NAgent.AgentCore.Security;
using NAgent.AgentCore.LLm;
using NAgent.AgentCore.Tools;

namespace NAgent.AgentCore.Agent;

/// <summary>
/// Agent 主运行器 - 协调整个 AI Agent 的执行流程
/// </summary>
public class AgentRunner
{
    private readonly LocalLlmClient _llmClient;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly PromptFilter _promptFilter;
    private readonly MemoryManager _memoryManager;
    private readonly SandboxResultCheck _resultCheck;

    public AgentRunner(
        LocalLlmClient llmClient,
        ToolDispatcher toolDispatcher,
        PromptFilter promptFilter,
        MemoryManager memoryManager,
        SandboxResultCheck resultCheck)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _toolDispatcher = toolDispatcher ?? throw new ArgumentNullException(nameof(toolDispatcher));
        _promptFilter = promptFilter ?? throw new ArgumentNullException(nameof(promptFilter));
        _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        _resultCheck = resultCheck ?? throw new ArgumentNullException(nameof(resultCheck));
    }

    /// <summary>
    /// 执行 Agent 任务
    /// </summary>
    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 入口安全层：过滤用户输入
            var filterResult = _promptFilter.Filter(request.UserInput);
            if (!filterResult.IsSafe)
            {
                return AgentResponse.Error($"安全拦截: {filterResult.Warning}");
            }

            // 2. 加载历史记忆
            var context = _memoryManager.LoadContext(request.SessionId);
            context.CurrentInput = filterResult.CleanedInput;

            // 3. LLM 解析意图和工具选择
            var intent = await _llmClient.ParseIntentAsync(context, cancellationToken);
            
            // 4. 工具分发执行
            var toolResult = await _toolDispatcher.DispatchAsync(intent, cancellationToken);

            // 5. 如果使用了沙箱，进行返回合规校验
            if (toolResult.UsedSandbox)
            {
                var validation = _resultCheck.Validate(toolResult.Output);
                if (!validation.IsPassed)
                {
                    return AgentResponse.Error($"沙箱结果校验失败: {string.Join("; ", validation.Warnings)}");
                }
            }

            // 6. 更新记忆
            _memoryManager.SaveContext(request.SessionId, context, toolResult.Output);

            // 7. 持久化（仅校验通过的数据）
            await PersistAsync(request.SessionId, context, toolResult.Output, cancellationToken);

            return AgentResponse.Success(toolResult.Output, toolResult.Metadata);
        }
        catch (Exception ex)
        {
            return AgentResponse.Error($"Agent 执行异常: {ex.Message}");
        }
    }

    private Task PersistAsync(string sessionId, AgentContext context, string output, CancellationToken cancellationToken)
    {
        // TODO: 实现数据持久化逻辑
        return Task.CompletedTask;
    }
}

/// <summary>
/// Agent 请求
/// </summary>
public record AgentRequest(string SessionId, string UserInput);

/// <summary>
/// Agent 响应
/// </summary>
public record AgentResponse(bool IsSuccess, string Output, Dictionary<string, object>? Metadata = null)
{
    public static AgentResponse Success(string output, Dictionary<string, object>? metadata = null)
        => new(true, output, metadata);

    public static AgentResponse Error(string message)
        => new(false, message);
}

/// <summary>
/// Agent 上下文
/// </summary>
public class AgentContext
{
    public string CurrentInput { get; set; } = string.Empty;
    public List<string> History { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}
