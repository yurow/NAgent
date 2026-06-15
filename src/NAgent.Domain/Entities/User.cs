using NAgent.Domain.Common;
using NAgent.Domain.Events;
using NAgent.Domain.Exceptions;
using SqlSugar;

namespace NAgent.Domain.Entities;

[SugarTable("Users")]
public class User : EntityBase
{
    [SugarColumn(IsPrimaryKey = true)]
    public new Guid Id { get; protected set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string Username { get; private set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Email { get; private set; }

    [SugarColumn(Length = 256, IsNullable = false)]
    public string PasswordHash { get; private set; }

    [SugarColumn(IsNullable = false)]
    public bool IsActive { get; private set; }

    [SugarColumn(IsNullable = false)]
    public bool IsAdmin { get; private set; }

    // ⭐ 标记为不映射到数据库（SqlSugar 会尝试序列化所有属性）
    [SugarColumn(IsIgnore = true)]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => base.DomainEvents;

    // ⭐ 基类的时间戳字段需要在子类中重新声明并配置
    [SugarColumn(IsNullable = false)]
    public new DateTime CreatedAt { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public new DateTime? UpdatedAt { get; protected set; }

    // SqlSugar 需要公共无参构造函数
    public User()
    {
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(string username, string email, string passwordHash, bool isAdmin = false)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
        IsAdmin = isAdmin;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(string username, string email)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("用户名不能为空");

        if (username.Length < 3 || username.Length > 50)
            throw new DomainException("用户名长度必须在3-50个字符之间");

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            throw new DomainException("邮箱格式不正确");

        var user = new User(username, email, string.Empty);
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Username, user.Email));
        
        return user;
    }

    /// <summary>
    /// 创建管理员用户（带密码）
    /// </summary>
    public static User CreateAdmin(string username, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("密码不能为空");

        var user = new User(username, email, passwordHash, isAdmin: true);
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Username, user.Email));
        
        return user;
    }

    /// <summary>
    /// 设置密码哈希
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("密码不能为空");
        
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail) || !IsValidEmail(newEmail))
            throw new DomainException("邮箱格式不正确");

        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdmin()
    {
        IsAdmin = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNormalUser()
    {
        IsAdmin = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("密码不能为空");

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
