namespace NAgent.AgentDomain.Entities;

/// <summary>
/// Agent 会话实体
/// </summary>
public class AgentSession
{
    public Guid Id { get; private set; }
    public string SessionKey { get; private set; }
    public Guid ProjectId { get; private set; }
    public List<ConversationMessage> Messages { get; private set; }
    public Dictionary<string, string> ContextVariables { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    private const int MaxMessages = 100; // 最大消息数

    private AgentSession()
    {
        SessionKey = string.Empty;
        Messages = new List<ConversationMessage>();
        ContextVariables = new Dictionary<string, string>();
    }

    public AgentSession(string sessionKey, Guid projectId)
    {
        Id = Guid.NewGuid();
        SessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
        ProjectId = projectId;
        Messages = new List<ConversationMessage>();
        ContextVariables = new Dictionary<string, string>();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加用户消息
    /// </summary>
    public void AddUserMessage(string content)
    {
        var message = new ConversationMessage(MessageRole.User, content);
        Messages.Add(message);
        TrimMessages();
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加助手消息
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        var message = new ConversationMessage(MessageRole.Assistant, content);
        Messages.Add(message);
        TrimMessages();
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置上下文变量
    /// </summary>
    public void SetContextVariable(string key, string value)
    {
        ContextVariables[key] = value;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取最近的N条消息
    /// </summary>
    public List<ConversationMessage> GetRecentMessages(int count = 10)
    {
        return Messages.TakeLast(count).ToList();
    }

    /// <summary>
    /// 修剪消息列表，保持不超过最大值
    /// </summary>
    private void TrimMessages()
    {
        if (Messages.Count > MaxMessages)
        {
            Messages = Messages.TakeLast(MaxMessages).ToList();
        }
    }
}

/// <summary>
/// 对话消息值对象
/// </summary>
public class ConversationMessage
{
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public DateTime Timestamp { get; private set; }

    private ConversationMessage() { }

    public ConversationMessage(MessageRole role, string content)
    {
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 消息角色
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System
}
