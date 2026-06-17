using SqlSugar;

namespace NAgent.AgentDomain.Entities;

/// <summary>
/// 知识图谱节点（实体）
/// </summary>
public class KnowledgeGraphNode
{
    /// <summary>
    /// 节点ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 所属项目ID
    /// </summary>
    [SugarColumn(Length = 36)]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// 实体名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型（如：人物、地点、组织、概念、技术、文件等）
    /// </summary>
    [SugarColumn(Length = 50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述/摘要
    /// </summary>
    [SugarColumn(IsNullable = true, Length = 2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 来源（如：用户上传文件、对话历史、搜索结果、手动输入）
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 来源标识（如文件路径、会话ID、搜索关键词）
    /// </summary>
    [SugarColumn(IsNullable = true, Length = 500)]
    public string? SourceId { get; set; }

    /// <summary>
    /// 出现次数（用于权重计算）
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识图谱边（关系）
/// </summary>
public class KnowledgeGraphEdge
{
    /// <summary>
    /// 边ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 所属项目ID
    /// </summary>
    [SugarColumn(Length = 36)]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// 源节点ID
    /// </summary>
    [SugarColumn(Length = 36)]
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID
    /// </summary>
    [SugarColumn(Length = 36)]
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型（如：包含、属于、使用、创建、相关、引用等）
    /// </summary>
    [SugarColumn(Length = 50)]
    public string RelationType { get; set; } = string.Empty;

    /// <summary>
    /// 关系描述
    /// </summary>
    [SugarColumn(IsNullable = true, Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识图谱查询结果
/// </summary>
public class KnowledgeGraphQueryResult
{
    /// <summary>
    /// 相关节点
    /// </summary>
    public List<KnowledgeGraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// 相关边
    /// </summary>
    public List<KnowledgeGraphEdge> Edges { get; set; } = new();

    /// <summary>
    /// 是否有结果
    /// </summary>
    public bool HasResults => Nodes.Count > 0;

    /// <summary>
    /// 生成文本摘要
    /// </summary>
    public string ToSummary()
    {
        if (!HasResults) return string.Empty;

        var lines = new List<string>();
        lines.Add($"涉及 {Nodes.Count} 个实体，{Edges.Count} 条关系：");
        lines.Add("");

        // 按类型分组显示实体
        var nodeGroups = Nodes.GroupBy(n => n.EntityType).OrderByDescending(g => g.Count());
        foreach (var group in nodeGroups)
        {
            lines.Add($"【{group.Key}】{string.Join("、", group.Select(n => n.Name))}");
        }

        // 显示关系
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
