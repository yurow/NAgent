using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 项目仓储接口
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// 根据ID获取项目
    /// </summary>
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有项目
    /// </summary>
    Task<List<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的活跃项目
    /// </summary>
    Task<Project?> GetActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取项目
    /// </summary>
    Task<Project?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查项目名称是否存在
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加项目
    /// </summary>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新项目
    /// </summary>
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除项目
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 激活项目
    /// </summary>
    Task ActivateProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停用项目
    /// </summary>
    Task DeactivateProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新项目最后访问时间
    /// </summary>
    Task UpdateLastAccessedAsync(Guid projectId, CancellationToken cancellationToken = default);
}