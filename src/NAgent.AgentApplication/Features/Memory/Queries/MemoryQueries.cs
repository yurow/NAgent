using MediatR;

namespace NAgent.AgentApplication.Features.Memory.Queries;

/// <summary>
/// 获取项目长期记忆摘要查询
/// </summary>
public record GetProjectMemorySummaryQuery(Guid ProjectId, int Limit = 20) : IRequest<List<ProjectMemorySummaryDto>>;

/// <summary>
/// 搜索项目长期记忆查询
/// </summary>
public record SearchProjectMemoryQuery(Guid ProjectId, string Query, int Limit = 10) : IRequest<List<ProjectMemorySummaryDto>>;

/// <summary>
/// 获取会话短期记忆查询
/// </summary>
public record GetSessionShortTermMemoryQuery(Guid ProjectId, Guid SessionId, int Limit = 10) : IRequest<List<MemoryEntryDto>>;

/// <summary>
/// 获取会话长期记忆查询
/// </summary>
public record GetSessionLongTermMemoryQuery(Guid ProjectId, Guid SessionId, int Limit = 20) : IRequest<List<MemoryEntryDto>>;

/// <summary>
/// 项目记忆摘要 DTO
/// </summary>
public class ProjectMemorySummaryDto
{
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Importance { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 记忆条目 DTO
/// </summary>
public class MemoryEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
