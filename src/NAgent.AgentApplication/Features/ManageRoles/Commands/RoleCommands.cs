using MediatR;

namespace NAgent.AgentApplication.Features.ManageRoles.Commands;

/// <summary>
/// 创建角色命令
/// </summary>
public record CreateRoleCommand(
    Guid ProjectId,
    string Name,
    string Description,
    string SystemPrompt,
    Guid? ModelId) : IRequest<Guid>;

/// <summary>
/// 更新角色命令
/// </summary>
public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string Description,
    string SystemPrompt,
    Guid? ModelId) : IRequest<bool>;

/// <summary>
/// 删除角色命令
/// </summary>
public record DeleteRoleCommand(Guid RoleId) : IRequest<bool>;

/// <summary>
/// 设置项目激活角色命令
/// </summary>
public record SetActiveRoleCommand(
    Guid ProjectId,
    Guid? RoleId) : IRequest<bool>;
