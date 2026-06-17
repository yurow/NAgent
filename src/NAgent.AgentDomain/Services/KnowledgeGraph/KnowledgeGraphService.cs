using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentDomain.Services.KnowledgeGraph;

/// <summary>
/// 知识图谱服务实现
/// 基于简单NLP提取器，无向量，纯规则匹配
/// </summary>
public class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly IKnowledgeGraphRepository _repository;
    private readonly SimpleNlpExtractor _nlpExtractor;

    public KnowledgeGraphService(IKnowledgeGraphRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _nlpExtractor = new SimpleNlpExtractor();
    }

    /// <summary>
    /// 从文本中提取实体和关系，构建知识图谱
    /// </summary>
    public async Task ExtractAndStoreAsync(
        string projectId,
        string text,
        string source,
        string? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // 1. 文本分块（避免单次处理过长文本）
        var chunks = _nlpExtractor.ChunkText(text, chunkSize: 2000, overlap: 200);

        foreach (var chunk in chunks)
        {
            // 2. 提取实体
            var entities = _nlpExtractor.ExtractEntities(chunk);

            // 3. 提取关系
            var relations = _nlpExtractor.ExtractRelations(chunk, entities);

            // 4. 存储实体（去重，已存在则增加出现次数）
            var entityNameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entity in entities)
            {
                var existingNode = await _repository.GetNodeByNameAsync(projectId, entity.Name, cancellationToken);
                if (existingNode != null)
                {
                    existingNode.OccurrenceCount += entity.OccurrenceCount;
                    existingNode.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateNodeAsync(existingNode, cancellationToken);
                    entityNameToId[entity.Name] = existingNode.Id;
                }
                else
                {
                    var newNode = new KnowledgeGraphNode
                    {
                        ProjectId = projectId,
                        Name = entity.Name,
                        EntityType = entity.EntityType,
                        Source = source,
                        SourceId = sourceId,
                        OccurrenceCount = entity.OccurrenceCount
                    };
                    await _repository.AddNodeAsync(newNode, cancellationToken);
                    entityNameToId[entity.Name] = newNode.Id;
                }
            }

            // 5. 存储关系
            foreach (var relation in relations)
            {
                if (!entityNameToId.TryGetValue(relation.SourceName, out var sourceId_) ||
                    !entityNameToId.TryGetValue(relation.TargetName, out var targetId_))
                    continue;

                var existingEdge = await _repository.GetEdgeAsync(sourceId_, targetId_, relation.RelationType, cancellationToken);
                if (existingEdge == null)
                {
                    var newEdge = new KnowledgeGraphEdge
                    {
                        ProjectId = projectId,
                        SourceNodeId = sourceId_,
                        TargetNodeId = targetId_,
                        RelationType = relation.RelationType,
                        Confidence = relation.Confidence,
                        Source = source
                    };
                    await _repository.AddEdgeAsync(newEdge, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// 根据查询关键词检索相关知识图谱信息
    /// </summary>
    public async Task<KnowledgeGraphQueryResult> QueryAsync(
        string projectId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new KnowledgeGraphQueryResult();

        // 1. 从查询中提取关键词
        var queryEntities = _nlpExtractor.ExtractEntities(query);
        var keywords = queryEntities.Select(e => e.Name).ToList();

        // 如果NLP没有提取到关键词，直接使用查询文本分词
        if (keywords.Count == 0)
        {
            keywords = query.Split(new[] { ' ', '，', '。', '、', '；', '：' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length >= 2)
                .ToList();
        }

        // 2. 查询相关知识图谱
        var result = await _repository.QueryRelatedAsync(projectId, keywords, depth: 1, cancellationToken);

        // 3. 限制结果数量
        result.Nodes = result.Nodes.Take(limit).ToList();
        result.Edges = result.Edges.Take(limit * 2).ToList();

        return result;
    }

    /// <summary>
    /// 获取项目的知识图谱摘要
    /// </summary>
    public async Task<string> GetProjectSummaryAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _repository.GetNodesByProjectAsync(projectId, cancellationToken);
        if (nodes.Count == 0)
            return "暂无知识图谱数据。";

        var lines = new List<string>
        {
            $"项目知识图谱：共 {nodes.Count} 个实体",
            ""
        };

        // 按类型统计
        var typeGroups = nodes.GroupBy(n => n.EntityType).OrderByDescending(g => g.Count());
        foreach (var group in typeGroups)
        {
            lines.Add($"【{group.Key}】({group.Count()}个)");
            foreach (var node in group.OrderByDescending(n => n.OccurrenceCount).Take(5))
            {
                lines.Add($"  - {node.Name} (出现{node.OccurrenceCount}次)");
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 删除指定来源的知识图谱数据
    /// </summary>
    public async Task DeleteBySourceAsync(
        string projectId,
        string source,
        string? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        await _repository.DeleteBySourceAsync(projectId, source, sourceId, cancellationToken);
    }
}
