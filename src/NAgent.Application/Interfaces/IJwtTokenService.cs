namespace NAgent.Application.Interfaces;

/// <summary>
/// JWT 令牌服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    string GenerateToken(Guid userId, string username, bool isAdmin);

    /// <summary>
    /// 验证 JWT Token
    /// </summary>
    bool ValidateToken(string token, out Guid userId, out string username, out bool isAdmin);
}
