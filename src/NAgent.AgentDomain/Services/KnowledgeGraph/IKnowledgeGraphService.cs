using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Services.KnowledgeGraph;

/// <summary>
/// 知识图谱服务接口
/// </summary>
public interface IKnowledgeGraphService
{
    /// <summary>
    /// 从文本中提取实体和关系，构建知识图谱
    /// </summary>
    Task ExtractAndStoreAsync(
        string projectId,
        string text,
        string source,
        string? sourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据查询关键词检索相关知识图谱信息
    /// </summary>
    Task<KnowledgeGraphQueryResult> QueryAsync(
        string projectId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目的知识图谱摘要
    /// </summary>
    Task<string> GetProjectSummaryAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定来源的知识图谱数据
    /// </summary>
    Task DeleteBySourceAsync(
        string projectId,
        string source,
        string? sourceId = null,
        CancellationToken cancellationToken = default);
}
