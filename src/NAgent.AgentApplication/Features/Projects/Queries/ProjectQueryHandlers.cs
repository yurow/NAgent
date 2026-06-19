using MediatR;
using NAgent.AgentApplication.Features.Projects.Queries;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.Projects.Queries;

/// <summary>
/// 获取用户项目列表查询处理器
/// </summary>
public class GetUserProjectsQueryHandler : IRequestHandler<GetUserProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public GetUserProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<List<ProjectDto>> Handle(GetUserProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            UserId = p.UserId,
            WorkspacePath = p.WorkspacePath,
            IsActive = p.IsActive,
            ActiveRoleId = p.ActiveRoleId,
            LastAccessedAt = p.LastAccessedAt,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            SessionCount = p.Sessions.Count
        }).ToList();
    }
}

/// <summary>
/// 获取活跃项目查询处理器
/// </summary>
public class GetActiveProjectQueryHandler : IRequestHandler<GetActiveProjectQuery, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;

    public GetActiveProjectQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<ProjectDto?> Handle(GetActiveProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetActiveProjectAsync(request.UserId, cancellationToken);

        if (project == null)
            return null;

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            UserId = project.UserId,
            WorkspacePath = project.WorkspacePath,
            IsActive = project.IsActive,
            ActiveRoleId = project.ActiveRoleId,
            LastAccessedAt = project.LastAccessedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            SessionCount = project.Sessions.Count
        };
    }
}

/// <summary>
/// 获取项目详情查询处理器
/// </summary>
public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project == null)
            return null;

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            UserId = project.UserId,
            WorkspacePath = project.WorkspacePath,
            IsActive = project.IsActive,
            ActiveRoleId = project.ActiveRoleId,
            LastAccessedAt = project.LastAccessedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            SessionCount = project.Sessions.Count
        };
    }
}

/// <summary>
/// 检查项目是否存在查询处理器
/// </summary>
public class ProjectExistsQueryHandler : IRequestHandler<ProjectExistsQuery, bool>
{
    private readonly IProjectRepository _projectRepository;

    public ProjectExistsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<bool> Handle(ProjectExistsQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.ExistsByNameAsync(request.UserId, request.ProjectName, cancellationToken);
    }
}