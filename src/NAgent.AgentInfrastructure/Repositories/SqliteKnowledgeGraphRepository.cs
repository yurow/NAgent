using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;
using SqlSugar;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// 知识图谱 SQLite 仓储实现
/// </summary>
public class SqliteKnowledgeGraphRepository : IKnowledgeGraphRepository
{
    private readonly ISqlSugarClient _db;

    public SqliteKnowledgeGraphRepository(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<KnowledgeGraphNode?> GetNodeByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<KnowledgeGraphNode>()
            .FirstAsync(n => n.ProjectId == projectId && n.Name == name, cancellationToken);
    }

    public async Task<List<KnowledgeGraphNode>> GetNodesByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<KnowledgeGraphNode>()
            .Where(n => n.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeGraphNode>> SearchNodesAsync(Guid projectId, string keyword, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<KnowledgeGraphNode>()
            .Where(n => n.ProjectId == projectId && (n.Name.Contains(keyword) || n.EntityType.Contains(keyword)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(node).ExecuteCommandAsync(cancellationToken);
    }

    public async Task UpdateNodeAsync(KnowledgeGraphNode node, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(node).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<KnowledgeGraphEdge?> GetEdgeAsync(Guid sourceNodeId, Guid targetNodeId, string relationType, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<KnowledgeGraphEdge>()
            .FirstAsync(e => e.SourceNodeId == sourceNodeId && e.TargetNodeId == targetNodeId && e.RelationType == relationType, cancellationToken);
    }

    public async Task<List<KnowledgeGraphEdge>> GetEdgesByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<KnowledgeGraphEdge>()
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddEdgeAsync(KnowledgeGraphEdge edge, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(edge).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<KnowledgeGraphQueryResult> QueryRelatedAsync(Guid projectId, List<string> keywords, int depth = 1, CancellationToken cancellationToken = default)
    {
        var result = new KnowledgeGraphQueryResult();
        if (keywords.Count == 0) return result;

        // 1. 找到匹配的节点
        var matchedNodes = new List<KnowledgeGraphNode>();
        foreach (var keyword in keywords)
        {
            var nodes = await _db.Queryable<KnowledgeGraphNode>()
                .Where(n => n.ProjectId == projectId && n.Name.Contains(keyword))
                .ToListAsync(cancellationToken);
            matchedNodes.AddRange(nodes);
        }

        matchedNodes = matchedNodes.DistinctBy(n => n.Id).ToList();
        if (matchedNodes.Count == 0) return result;

        result.Nodes.AddRange(matchedNodes);

        // 2. 找到与这些节点相关的边（depth=1）
        var nodeIds = matchedNodes.Select(n => n.Id).ToList();
        var edges = await _db.Queryable<KnowledgeGraphEdge>()
            .Where(e => e.ProjectId == projectId && (nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId)))
            .ToListAsync(cancellationToken);

        result.Edges.AddRange(edges);

        // 3. 找到边连接的其它节点
        var relatedNodeIds = edges.Select(e => e.SourceNodeId)
            .Concat(edges.Select(e => e.TargetNodeId))
            .Distinct()
            .Where(id => !nodeIds.Contains(id))
            .ToList();

        if (relatedNodeIds.Count > 0)
        {
            var relatedNodes = await _db.Queryable<KnowledgeGraphNode>()
                .Where(n => relatedNodeIds.Contains(n.Id))
                .ToListAsync(cancellationToken);
            result.Nodes.AddRange(relatedNodes);
        }

        return result;
    }

    public async Task<List<KnowledgeGraphNode>> GetNodesBySourceAsync(Guid projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<KnowledgeGraphNode>()
            .Where(n => n.ProjectId == projectId && n.Source == source);

        if (!string.IsNullOrEmpty(sourceId))
        {
            query = query.Where(n => n.SourceId == sourceId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task DeleteBySourceAsync(Guid projectId, string source, string? sourceId = null, CancellationToken cancellationToken = default)
    {
        // 1. 找到要删除的节点
        var query = _db.Queryable<KnowledgeGraphNode>()
            .Where(n => n.ProjectId == projectId && n.Source == source);

        if (!string.IsNullOrEmpty(sourceId))
        {
            query = query.Where(n => n.SourceId == sourceId);
        }

        var nodesToDelete = await query.ToListAsync(cancellationToken);
        var nodeIds = nodesToDelete.Select(n => n.Id).ToList();

        if (nodeIds.Count == 0) return;

        // 2. 删除相关边
        await _db.Deleteable<KnowledgeGraphEdge>()
            .Where(e => nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId))
            .ExecuteCommandAsync(cancellationToken);

        // 3. 删除节点
        await _db.Deleteable<KnowledgeGraphNode>()
            .Where(n => nodeIds.Contains(n.Id))
            .ExecuteCommandAsync(cancellationToken);
    }
}
