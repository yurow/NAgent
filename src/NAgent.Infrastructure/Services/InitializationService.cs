using System.Text;
using Microsoft.Extensions.Hosting;
using NAgent.Application.Interfaces;
using NAgent.Domain.Entities;
using NAgent.Domain.Repositories;

namespace NAgent.Infrastructure.Services;

/// <summary>
/// 系统初始化服务实现
/// </summary>
public class InitializationService : IInitializationService
{
    private readonly IUserRepository _userRepository;
    private readonly IHostEnvironment _environment;
    private readonly IPasswordHasher _passwordHasher;
    private const string InitFlagFileName = ".initialized";

    public InitializationService(
        IUserRepository userRepository,
        IHostEnvironment environment,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>
    /// 检查系统是否已初始化
    /// </summary>
    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default)
    {
        // 1. 检查初始化标识文件
        var initFlagPath = Path.Combine(_environment.ContentRootPath, InitFlagFileName);
        if (File.Exists(initFlagPath))
        {
            return true;
        }

        // 2. 检查是否存在管理员账号（双重验证）
        var adminExists = await _userRepository.ExistsByUsernameAsync("admin", cancellationToken);
        return adminExists;
    }

    /// <summary>
    /// 执行系统初始化（创建管理员账号）
    /// </summary>
    public async Task InitializeAsync(string adminUsername, string adminEmail, string adminPassword, CancellationToken cancellationToken = default)
    {
        // 1. 验证是否已初始化
        if (await IsInitializedAsync(cancellationToken))
        {
            throw new InvalidOperationException("系统已初始化，无法重复初始化");
        }

        // 2. 验证输入参数
        if (string.IsNullOrWhiteSpace(adminUsername))
            throw new ArgumentException("管理员用户名不能为空", nameof(adminUsername));

        if (string.IsNullOrWhiteSpace(adminEmail))
            throw new ArgumentException("管理员邮箱不能为空", nameof(adminEmail));

        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 6)
            throw new ArgumentException("密码长度至少为6个字符", nameof(adminPassword));

        // 3. 检查用户名和邮箱是否已存在
        if (await _userRepository.ExistsByUsernameAsync(adminUsername, cancellationToken))
            throw new InvalidOperationException($"用户名 '{adminUsername}' 已存在");

        if (await _userRepository.ExistsByEmailAsync(adminEmail, cancellationToken))
            throw new InvalidOperationException($"邮箱 '{adminEmail}' 已被使用");

        // 4. 哈希密码
        var passwordHash = _passwordHasher.HashPassword(adminPassword);

        // 5. 创建管理员用户
        var adminUser = User.CreateAdmin(adminUsername, adminEmail, passwordHash);
        await _userRepository.AddAsync(adminUser, cancellationToken);

        // 6. 创建初始化标识文件
        var initFlagPath = Path.Combine(_environment.ContentRootPath, InitFlagFileName);
        var initInfo = new
        {
            InitializedAt = DateTime.UtcNow,
            AdminUsername = adminUsername,
            Version = "1.0.0"
        };
        
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(initInfo, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(initFlagPath, jsonContent, Encoding.UTF8, cancellationToken);
    }
}
