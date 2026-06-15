using MediatR;
using NAgent.Application.Interfaces;
using NAgent.Domain.Entities;
using NAgent.Domain.Exceptions;
using NAgent.Domain.Repositories;

namespace NAgent.Application.Features.Auth.Queries;

/// <summary>
/// 用户登录查询处理器
/// </summary>
public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginQueryHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<LoginResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(request.Username))
            return new LoginResult(false, ErrorMessage: "用户名不能为空");

        if (string.IsNullOrWhiteSpace(request.Password))
            return new LoginResult(false, ErrorMessage: "密码不能为空");

        // 查找用户
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null)
        {
            return new LoginResult(false, ErrorMessage: "用户名或密码错误");
        }

        // 验证密码
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new LoginResult(false, ErrorMessage: "用户名或密码错误");
        }

        // 检查账号是否激活
        if (!user.IsActive)
        {
            return new LoginResult(false, ErrorMessage: "账号已被禁用");
        }

        // 生成 JWT Token
        var token = _jwtTokenService.GenerateToken(user.Id, user.Username, user.IsAdmin);

        return new LoginResult(
            true,
            Token: token,
            User: new UserInfo(user.Id, user.Username, user.Email, user.IsAdmin)
        );
    }
}
