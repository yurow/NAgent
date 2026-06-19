using MediatR;

namespace NAgent.AgentApplication.Features.ManageRoles.Queries;

/// <summary>
/// 获取项目所有角色查询
/// </summary>
public class GetProjectRolesQuery : IRequest<List<AgentRoleDto>>
{
    public Guid ProjectId { get; set; }
}

/// <summary>
/// 获取角色详情查询
/// </summary>
public class GetRoleByIdQuery : IRequest<AgentRoleDto?>
{
    public Guid RoleId { get; set; }
}

/// <summary>
/// 角色 DTO
/// </summary>
public class AgentRoleDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public Guid? ModelId { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
