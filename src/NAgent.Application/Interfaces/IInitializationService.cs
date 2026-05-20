namespace NAgent.Application.Interfaces;

/// <summary>
/// 系统初始化服务接口
/// </summary>
public interface IInitializationService
{
    /// <summary>
    /// 检查系统是否已初始化
    /// </summary>
    Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行系统初始化（创建管理员账号）
    /// </summary>
    Task InitializeAsync(string adminUsername, string adminEmail, string adminPassword, CancellationToken cancellationToken = default);
}
