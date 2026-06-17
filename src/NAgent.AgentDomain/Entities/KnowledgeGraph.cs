namespace NAgent.AgentDomain.Entities;

/// <summary>
/// 知识图谱节点（实体）
/// </summary>
public class KnowledgeGraphNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识图谱边（关系）
/// </summary>
public class KnowledgeGraphEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Source { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识图谱查询结果
/// </summary>
public class KnowledgeGraphQueryResult
{
    public List<KnowledgeGraphNode> Nodes { get; set; } = new();
    public List<KnowledgeGraphEdge> Edges { get; set; } = new();

    public bool HasResults => Nodes.Count > 0;

    public string ToSummary()
    {
        if (!HasResults) return string.Empty;

        var lines = new List<string>();
        lines.Add($"涉及 {Nodes.Count} 个实体，{Edges.Count} 条关系：");
        lines.Add("");

        var nodeGroups = Nodes.GroupBy(n => n.EntityType).OrderByDescending(g => g.Count());
        foreach (var group in nodeGroups)
        {
            lines.Add($"【{group.Key}】{string.Join("、", group.Select(n => n.Name))}");
        }

        if (Edges.Count > 0)
        {
            lines.Add("");
            lines.Add("【关系】");
            var nodeDict = Nodes.ToDictionary(n => n.Id, n => n.Name);
            foreach (var edge in Edges.Take(10))
            {
                var sourceName = nodeDict.GetValueOrDefault(edge.SourceNodeId, "?");
                var targetName = nodeDict.GetValueOrDefault(edge.TargetNodeId, "?");
                lines.Add($"- {sourceName} --[{edge.RelationType}]--> {targetName}");
            }
        }

        return string.Join("\n", lines);
    }
}
