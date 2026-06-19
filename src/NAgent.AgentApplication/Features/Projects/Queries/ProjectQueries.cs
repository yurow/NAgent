using MediatR;

namespace NAgent.AgentApplication.Features.Projects.Queries;

/// <summary>
/// 获取用户项目列表查询
/// </summary>
public class GetUserProjectsQuery : IRequest<List<ProjectDto>>
{
    public Guid UserId { get; set; }
}

/// <summary>
/// 获取活跃项目查询
/// </summary>
public class GetActiveProjectQuery : IRequest<ProjectDto?>
{
    public Guid UserId { get; set; }
}

/// <summary>
/// 获取项目详情查询
/// </summary>
public class GetProjectByIdQuery : IRequest<ProjectDto?>
{
    public Guid ProjectId { get; set; }
}

/// <summary>
/// 检查项目是否存在查询
/// </summary>
public class ProjectExistsQuery : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string ProjectName { get; set; }
}

/// <summary>
/// 项目 DTO
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid UserId { get; set; }
    public string WorkspacePath { get; set; }
    public bool IsActive { get; set; }
    public Guid? ActiveRoleId { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SessionCount { get; set; }
}