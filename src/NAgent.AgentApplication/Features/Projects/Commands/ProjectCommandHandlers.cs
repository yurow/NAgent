using MediatR;
using NAgent.AgentApplication.Features.Projects.Commands;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.Projects.Commands;

/// <summary>
/// 创建项目命令处理器
/// </summary>
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceManager _workspaceManager;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IWorkspaceManager workspaceManager)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _projectRepository.ExistsByNameAsync(request.UserId, request.Name, cancellationToken))
            throw new ArgumentException($"项目名称 '{request.Name}' 已存在", nameof(request.Name));

        var projectId = Guid.NewGuid();
        var workspacePath = _workspaceManager.GetProjectWorkspacePath(request.UserId, projectId);

        var project = Project.Create(
            request.Name,
            request.Description,
            request.UserId,
            workspacePath,
            projectId
        );

        await _projectRepository.AddAsync(project, cancellationToken);

        _workspaceManager.CreateProjectConfig(
            request.UserId,
            project.Id,
            project.Name,
            project.Description
        );

        return project.Id;
    }
}

/// <summary>
/// 激活项目命令处理器
/// </summary>
public class ActivateProjectCommandHandler : IRequestHandler<ActivateProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;

    public ActivateProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<bool> Handle(ActivateProjectCommand request, CancellationToken cancellationToken)
    {
        await _projectRepository.ActivateProjectAsync(request.ProjectId, cancellationToken);
        return true;
    }
}

/// <summary>
/// 停用项目命令处理器
/// </summary>
public class DeactivateProjectCommandHandler : IRequestHandler<DeactivateProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;

    public DeactivateProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<bool> Handle(DeactivateProjectCommand request, CancellationToken cancellationToken)
    {
        await _projectRepository.DeactivateProjectAsync(request.ProjectId, cancellationToken);
        return true;
    }
}

/// <summary>
/// 更新项目命令处理器
/// </summary>
public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;

    public UpdateProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<bool> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new ArgumentException($"项目 {request.ProjectId} 不存在", nameof(request.ProjectId));

        project.Update(request.Name, request.Description);
        await _projectRepository.UpdateAsync(project, cancellationToken);

        return true;
    }
}

/// <summary>
/// 删除项目命令处理器
/// </summary>
public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceManager _workspaceManager;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IWorkspaceManager workspaceManager)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
    }

    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new ArgumentException($"项目 {request.ProjectId} 不存在", nameof(request.ProjectId));

        await _projectRepository.DeleteAsync(request.ProjectId, cancellationToken);

        _workspaceManager.DeleteProjectWorkspace(request.UserId, request.ProjectId);

        return true;
    }
}
