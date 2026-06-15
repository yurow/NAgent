using MediatR;

namespace NAgent.Application.Features.Users.Commands;

/// <summary>
/// 更新用户状态命令
/// </summary>
public record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest<bool>;

/// <summary>
/// 更新用户角色命令
/// </summary>
public record UpdateUserRoleCommand(Guid UserId, bool IsAdmin) : IRequest<bool>;

/// <summary>
/// 重置用户密码命令
/// </summary>
public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;
