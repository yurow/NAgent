using MediatR;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.ManageRoles.Queries;

/// <summary>
/// 获取项目角色列表查询处理器
/// </summary>
public class GetProjectRolesQueryHandler : IRequestHandler<GetProjectRolesQuery, List<AgentRoleDto>>
{
    private readonly IAgentRoleRepository _roleRepository;

    public GetProjectRolesQueryHandler(IAgentRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<List<AgentRoleDto>> Handle(GetProjectRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return roles.Select(r => new AgentRoleDto
        {
            Id = r.Id,
            ProjectId = r.ProjectId,
            Name = r.Name,
            Description = r.Description,
            SystemPrompt = r.SystemPrompt,
            ModelId = r.ModelId,
            IsActive = r.IsActive,
            SortOrder = r.SortOrder,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }
}

/// <summary>
/// 获取角色详情查询处理器
/// </summary>
public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, AgentRoleDto?>
{
    private readonly IAgentRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IAgentRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<AgentRoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
            return null;

        return new AgentRoleDto
        {
            Id = role.Id,
            ProjectId = role.ProjectId,
            Name = role.Name,
            Description = role.Description,
            SystemPrompt = role.SystemPrompt,
            ModelId = role.ModelId,
            IsActive = role.IsActive,
            SortOrder = role.SortOrder,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
