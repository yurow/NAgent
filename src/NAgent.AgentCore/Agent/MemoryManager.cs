namespace NAgent.AgentCore.Agent;

/// <summary>
/// 记忆管理器 - 管理 Agent 的长短记忆
/// </summary>
public class MemoryManager
{
    private readonly Dictionary<string, AgentContext> _shortTermMemory = new();
    private readonly ILongTermStorage? _longTermStorage;

    public MemoryManager(ILongTermStorage? longTermStorage = null)
    {
        _longTermStorage = longTermStorage;
    }

    /// <summary>
    /// 加载会话上下文
    /// </summary>
    public AgentContext LoadContext(string sessionId)
    {
        if (_shortTermMemory.TryGetValue(sessionId, out var context))
        {
            return context;
        }

        // 尝试从长期存储加载
        if (_longTermStorage != null)
        {
            var savedContext = _longTermStorage.Load(sessionId);
            if (savedContext != null)
            {
                _shortTermMemory[sessionId] = savedContext;
                return savedContext;
            }
        }

        // 创建新的上下文
        var newContext = new AgentContext();
        _shortTermMemory[sessionId] = newContext;
        return newContext;
    }

    /// <summary>
    /// 保存会话上下文
    /// </summary>
    public void SaveContext(string sessionId, AgentContext context, string latestOutput)
    {
        // 更新短期记忆
        context.History.Add(latestOutput);
        
        // 限制历史记录长度（避免内存溢出）
        if (context.History.Count > 50)
        {
            context.History.RemoveAt(0);
        }

        _shortTermMemory[sessionId] = context;

        // 异步保存到长期存储
        if (_longTermStorage != null)
        {
            _ = Task.Run(() => _longTermStorage.Save(sessionId, context));
        }
    }

    /// <summary>
    /// 清除会话短期记忆
    /// </summary>
    public void ClearShortTermMemory(string sessionId)
    {
        _shortTermMemory.Remove(sessionId);
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    public List<string> GetHistory(string sessionId, int maxCount = 10)
    {
        if (!_shortTermMemory.TryGetValue(sessionId, out var context))
        {
            return new List<string>();
        }

        return context.History.TakeLast(maxCount).ToList();
    }
}

/// <summary>
/// 长期存储接口
/// </summary>
public interface ILongTermStorage
{
    AgentContext? Load(string sessionId);
    Task Save(string sessionId, AgentContext context);
}
