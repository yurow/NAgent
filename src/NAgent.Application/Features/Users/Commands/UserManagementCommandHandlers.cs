using MediatR;
using NAgent.Application.Interfaces;
using NAgent.Domain.Entities;
using NAgent.Domain.Exceptions;
using NAgent.Domain.Repositories;

namespace NAgent.Application.Features.Users.Commands;

/// <summary>
/// 更新用户状态命令处理器
/// </summary>
public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserStatusCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<bool> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"用户 {request.UserId} 不存在");

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        _userRepository.Update(user);
        return true;
    }
}

/// <summary>
/// 更新用户角色命令处理器
/// </summary>
public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserRoleCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"用户 {request.UserId} 不存在");

        if (request.IsAdmin)
            user.SetAdmin();
        else
            user.SetNormalUser();

        _userRepository.Update(user);
        return true;
    }
}

/// <summary>
/// 重置用户密码命令处理器
/// </summary>
public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetUserPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<bool> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"用户 {request.UserId} 不存在");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new DomainException("密码长度至少为6个字符");

        var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(passwordHash);

        _userRepository.Update(user);
        return true;
    }
}
