namespace NAgent.AgentDomain.Events;

/// <summary>
/// 项目创建事件
/// </summary>
public class ProjectCreatedEvent : IDomainEvent
{
    public Guid ProjectId { get; }
    public string ProjectName { get; }
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public ProjectCreatedEvent(Guid projectId, string projectName, Guid userId)
    {
        ProjectId = projectId;
        ProjectName = projectName;
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// 项目激活事件
/// </summary>
public class ProjectActivatedEvent : IDomainEvent
{
    public Guid ProjectId { get; }
    public string ProjectName { get; }
    public DateTime OccurredOn { get; }

    public ProjectActivatedEvent(Guid projectId, string projectName)
    {
        ProjectId = projectId;
        ProjectName = projectName;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// 会话记忆更新事件
/// </summary>
public class SessionMemoryUpdatedEvent : IDomainEvent
{
    public Guid SessionId { get; }
    public Guid ProjectId { get; }
    public string MemoryType { get; }
    public DateTime OccurredOn { get; }

    public SessionMemoryUpdatedEvent(Guid sessionId, Guid projectId, string memoryType)
    {
        SessionId = sessionId;
        ProjectId = projectId;
        MemoryType = memoryType;
        OccurredOn = DateTime.UtcNow;
    }
}