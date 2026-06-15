using MediatR;
using NAgent.AgentApplication.Features.Memory.Queries;
using NAgent.AgentDomain.Services.Memory;

namespace NAgent.AgentApplication.Features.Memory.Queries;

/// <summary>
/// 获取项目长期记忆摘要查询处理器
/// </summary>
public class GetProjectMemorySummaryQueryHandler : IRequestHandler<GetProjectMemorySummaryQuery, List<ProjectMemorySummaryDto>>
{
    private readonly IMemorySystem _memorySystem;

    public GetProjectMemorySummaryQueryHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<List<ProjectMemorySummaryDto>> Handle(GetProjectMemorySummaryQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _memorySystem.GetProjectMemorySummaryAsync(request.ProjectId, request.Limit, cancellationToken);
        return summaries.Select(s => new ProjectMemorySummaryDto
        {
            Content = s.Content,
            Summary = s.Summary,
            Category = s.Category,
            Importance = s.Importance,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}

/// <summary>
/// 搜索项目长期记忆查询处理器
/// </summary>
public class SearchProjectMemoryQueryHandler : IRequestHandler<SearchProjectMemoryQuery, List<ProjectMemorySummaryDto>>
{
    private readonly IMemorySystem _memorySystem;

    public SearchProjectMemoryQueryHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<List<ProjectMemorySummaryDto>> Handle(SearchProjectMemoryQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _memorySystem.SearchProjectLongTermMemoryAsync(request.ProjectId, request.Query, request.Limit, cancellationToken);
        return summaries.Select(s => new ProjectMemorySummaryDto
        {
            Content = s.Content,
            Summary = s.Summary,
            Category = s.Category,
            Importance = s.Importance,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}

/// <summary>
/// 获取会话短期记忆查询处理器
/// </summary>
public class GetSessionShortTermMemoryQueryHandler : IRequestHandler<GetSessionShortTermMemoryQuery, List<MemoryEntryDto>>
{
    private readonly IMemorySystem _memorySystem;

    public GetSessionShortTermMemoryQueryHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<List<MemoryEntryDto>> Handle(GetSessionShortTermMemoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _memorySystem.GetShortTermMemoryAsync(request.ProjectId, request.SessionId, request.Limit, cancellationToken);
        return entries.Select(e => new MemoryEntryDto
        {
            Id = e.Id,
            Role = e.Role,
            Content = e.Content,
            Timestamp = e.Timestamp
        }).ToList();
    }
}

/// <summary>
/// 获取会话长期记忆查询处理器
/// </summary>
public class GetSessionLongTermMemoryQueryHandler : IRequestHandler<GetSessionLongTermMemoryQuery, List<MemoryEntryDto>>
{
    private readonly IMemorySystem _memorySystem;

    public GetSessionLongTermMemoryQueryHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<List<MemoryEntryDto>> Handle(GetSessionLongTermMemoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _memorySystem.GetLongTermMemoryAsync(request.ProjectId, request.SessionId, request.Limit, cancellationToken);
        return entries.Select(e => new MemoryEntryDto
        {
            Id = e.Id,
            Role = e.Role,
            Content = e.Content,
            Timestamp = e.Timestamp
        }).ToList();
    }
}
