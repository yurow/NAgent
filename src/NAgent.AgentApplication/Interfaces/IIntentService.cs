using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Interfaces;

/// <summary>
/// 意图分类与推测服务接口
/// </summary>
public interface IIntentService
{
    /// <summary>
    /// 轻量意图分类：仅根据用户最新输入 + 简短对话摘要，返回固定意图枚举标识
    /// </summary>
    Task<IntentClassificationResult> ClassifyIntentAsync(
        string userInput,
        List<ChatSummary> recentSummaries,
        string? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 深度意图推测：结合短期对话记忆 + 长期知识库检索 + 全部可用 Skills/Tools，推测用户真实意图
    /// </summary>
    Task<IntentInferenceResult> InferIntentAsync(
        string userInput,
        Guid projectId,
        string sessionKey,
        List<ChatMessageDto> shortTermMessages,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 对话摘要（用于意图分类的轻量上下文）
/// </summary>
public class ChatSummary
{
    public string Role { get; set; } = "";
    public string Summary { get; set; } = "";
}

/// <summary>
/// 意图分类结果
/// </summary>
public class IntentClassificationResult
{
    /// <summary>
    /// 意图英文标识
    /// </summary>
    public string Intent { get; set; } = "general_chat";

    /// <summary>
    /// 意图中文描述
    /// </summary>
    public string Description { get; set; } = "一般对话";

    /// <summary>
    /// 置信度 0-1
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// 意图推测结果
/// </summary>
public class IntentInferenceResult
{
    /// <summary>
    /// 推测的意图标识
    /// </summary>
    public string Intent { get; set; } = "general_chat";

    /// <summary>
    /// 意图中文描述
    /// </summary>
    public string Description { get; set; } = "一般对话";

    /// <summary>
    /// 推测依据说明
    /// </summary>
    public string Reasoning { get; set; } = "";

    /// <summary>
    /// 推荐使用的工具/技能名称列表
    /// </summary>
    public List<string> RecommendedTools { get; set; } = new();

    /// <summary>
    /// 推荐使用的技能名称列表
    /// </summary>
    public List<string> RecommendedSkills { get; set; } = new();

    /// <summary>
    /// 置信度 0-1
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 检索到的相关知识片段
    /// </summary>
    public List<string> KnowledgeSnippets { get; set; } = new();
}
