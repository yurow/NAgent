using System.Security.Cryptography;
using System.Text;
using NAgent.Application.Interfaces;

namespace NAgent.Infrastructure.Services;

/// <summary>
/// SHA256 密码哈希服务实现
/// TODO: 生产环境应迁移至 BCrypt 或 PBKDF2 等更安全的算法
/// </summary>
public class Sha256PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// 使用 SHA256 哈希密码
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var hashedInput = HashPassword(password);
        return hashedInput == passwordHash;
    }
}
