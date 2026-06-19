using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IAgentRoleRepository
{
    /// <summary>
    /// 根据 ID 获取角色
    /// </summary>
    Task<AgentRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目的所有角色
    /// </summary>
    Task<List<AgentRole>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加角色
    /// </summary>
    Task AddAsync(AgentRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task UpdateAsync(AgentRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
