using MediatR;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageRoles.Commands;

/// <summary>
/// 创建角色命令处理器
/// </summary>
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IAgentRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IAgentRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = AgentRole.Create(
            request.ProjectId,
            request.Name,
            request.Description,
            request.SystemPrompt,
            request.ModelId);

        await _roleRepository.AddAsync(role, cancellationToken);
        return role.Id;
    }
}

/// <summary>
/// 更新角色命令处理器
/// </summary>
public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, bool>
{
    private readonly IAgentRoleRepository _roleRepository;

    public UpdateRoleCommandHandler(IAgentRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
            return false;

        role.Update(request.Name, request.Description, request.SystemPrompt, request.ModelId);
        await _roleRepository.UpdateAsync(role, cancellationToken);
        return true;
    }
}

/// <summary>
/// 删除角色命令处理器
/// </summary>
public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IAgentRoleRepository _roleRepository;

    public DeleteRoleCommandHandler(IAgentRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
            return false;

        await _roleRepository.DeleteAsync(request.RoleId, cancellationToken);
        return true;
    }
}

/// <summary>
/// 设置激活角色命令处理器
/// </summary>
public class SetActiveRoleCommandHandler : IRequestHandler<SetActiveRoleCommand, bool>
{
    private readonly IProjectRepository _projectRepository;

    public SetActiveRoleCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<bool> Handle(SetActiveRoleCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            return false;

        project.SetActiveRole(request.RoleId);
        await _projectRepository.UpdateAsync(project, cancellationToken);
        return true;
    }
}
