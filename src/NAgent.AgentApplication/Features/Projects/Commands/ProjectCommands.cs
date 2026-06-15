using MediatR;

namespace NAgent.AgentApplication.Features.Projects.Commands;

/// <summary>
/// 创建项目命令
/// </summary>
public record CreateProjectCommand(string Name, string Description, Guid UserId) : IRequest<Guid>;

/// <summary>
/// 激活项目命令
/// </summary>
public record ActivateProjectCommand(Guid ProjectId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 停用项目命令
/// </summary>
public record DeactivateProjectCommand(Guid ProjectId) : IRequest<bool>;

/// <summary>
/// 更新项目命令
/// </summary>
public record UpdateProjectCommand(Guid ProjectId, string Name, string Description) : IRequest<bool>;

/// <summary>
/// 删除项目命令
/// </summary>
public record DeleteProjectCommand(Guid ProjectId, Guid UserId) : IRequest<bool>;
