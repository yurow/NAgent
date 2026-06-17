using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 知识图谱仓储接口
/// </summary>
public interface IKnowledgeGraphRepository
{
    // 节点操作
    Task<KnowledgeGraphNode?> GetNodeByNameAsync(string projectId, string name, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> GetNodesByProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> SearchNodesAsync(string projectId, string keyword, int limit = 20, CancellationToken cancellationToken = default);
    Task AddNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default);
    Task UpdateNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default);

    // 边操作
    Task<KnowledgeGraphEdge?> GetEdgeAsync(string sourceNodeId, string targetNodeId, string relationType, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphEdge>> GetEdgesByNodeAsync(string nodeId, CancellationToken cancellationToken = default);
    Task AddEdgeAsync(KnowledgeGraphEdge edge, CancellationToken cancellationToken = default);

    // 查询
    Task<KnowledgeGraphQueryResult> QueryRelatedAsync(string projectId, List<string> keywords, int depth = 1, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> GetNodesBySourceAsync(string projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default);

    // 删除
    Task DeleteBySourceAsync(string projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default);
}
