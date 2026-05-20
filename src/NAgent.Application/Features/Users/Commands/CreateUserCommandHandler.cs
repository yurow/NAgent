using MediatR;
using NAgent.Domain.Entities;
using NAgent.Domain.Exceptions;
using NAgent.Domain.Repositories;

namespace NAgent.Application.Features.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 验证用户名是否已存在
        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
            throw new DomainException($"用户名 '{request.Username}' 已存在");

        // 验证邮箱是否已存在
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new DomainException($"邮箱 '{request.Email}' 已被注册");

        // 创建领域实体（会触发业务规则验证和领域事件）
        var user = User.Create(request.Username, request.Email);

        // 持久化
        await _userRepository.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
