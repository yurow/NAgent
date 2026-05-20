using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Events;

/// <summary>
/// Agent 会话创建事件
/// </summary>
public class AgentSessionCreatedEvent : IDomainEvent
{
    public Guid SessionId { get; }
    public string SessionKey { get; }
    public DateTime OccurredOn { get; }

    public AgentSessionCreatedEvent(Guid sessionId, string sessionKey)
    {
        SessionId = sessionId;
        SessionKey = sessionKey;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Agent 工具执行事件
/// </summary>
public class AgentToolExecutedEvent : IDomainEvent
{
    public string ToolName { get; }
    public bool Success { get; }
    public string Output { get; }
    public TimeSpan ExecutionTime { get; }
    public DateTime OccurredOn { get; }

    public AgentToolExecutedEvent(string toolName, bool success, string output, TimeSpan executionTime)
    {
        ToolName = toolName;
        Success = success;
        Output = output;
        ExecutionTime = executionTime;
        OccurredOn = DateTime.UtcNow;
    }
}
