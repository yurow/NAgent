using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 项目长期记忆仓储接口
/// </summary>
public interface IProjectMemoryRepository
{
    Task<ProjectMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemory>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemory>> GetByProjectIdAsync(Guid projectId, MemoryCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemory>> GetActiveMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemory>> SearchAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemory>> GetTopMemoriesAsync(Guid projectId, int limit = 20, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMemory memory, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectMemory memory, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task CleanupExpiredMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default);
}
