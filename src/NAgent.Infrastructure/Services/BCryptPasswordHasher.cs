using BCrypt.Net;
using NAgent.Application.Interfaces;

namespace NAgent.Infrastructure.Services;

/// <summary>
/// BCrypt 密码哈希服务实现
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// 使用 BCrypt 哈希密码（自动生成盐值）
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // 兼容旧的 SHA256 哈希格式（Base64 编码，长度固定）
        // 如果不是 BCrypt 格式（不以 $2$ 开头），则使用旧方式验证
        if (!passwordHash.StartsWith("$2"))
        {
            // 旧 SHA256 兼容验证
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            var oldHash = Convert.ToBase64String(hashedBytes);
            return oldHash == passwordHash;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
