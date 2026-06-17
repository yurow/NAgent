using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 知识图谱仓储接口
/// </summary>
public interface IKnowledgeGraphRepository
{
    // 节点操作
    Task<KnowledgeGraphNode?> GetNodeByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> GetNodesByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> SearchNodesAsync(Guid projectId, string keyword, int limit = 20, CancellationToken cancellationToken = default);
    Task AddNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default);
    Task UpdateNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default);

    // 边操作
    Task<KnowledgeGraphEdge?> GetEdgeAsync(Guid sourceNodeId, Guid targetNodeId, string relationType, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphEdge>> GetEdgesByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task AddEdgeAsync(KnowledgeGraphEdge edge, CancellationToken cancellationToken = default);

    // 查询
    Task<KnowledgeGraphQueryResult> QueryRelatedAsync(Guid projectId, List<string> keywords, int depth = 1, CancellationToken cancellationToken = default);
    Task<List<KnowledgeGraphNode>> GetNodesBySourceAsync(Guid projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default);

    // 删除
    Task DeleteBySourceAsync(Guid projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default);
}
