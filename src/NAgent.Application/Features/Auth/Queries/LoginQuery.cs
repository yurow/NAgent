using MediatR;

namespace NAgent.Application.Features.Auth.Queries;

/// <summary>
/// 用户登录查询
/// </summary>
public record LoginQuery(string Username, string Password) : IRequest<LoginResult>;

/// <summary>
/// 登录结果
/// </summary>
public record LoginResult(
    bool Success,
    string? Token = null,
    string? ErrorMessage = null,
    UserInfo? User = null);

/// <summary>
/// 用户信息 - 属性名使用 camelCase 以匹配前端 JavaScript
/// </summary>
public class UserInfo
{
    public Guid userId { get; set; }
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool isAdmin { get; set; }

    public UserInfo() { }

    public UserInfo(Guid userId, string username, string email, bool isAdmin)
    {
        this.userId = userId;
        this.username = username;
        this.email = email;
        this.isAdmin = isAdmin;
    }
}
