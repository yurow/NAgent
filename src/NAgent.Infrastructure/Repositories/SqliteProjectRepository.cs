using SqlSugar;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.Infrastructure.Repositories;

/// <summary>
/// 项目仓储实现（SQLite 持久化）
/// </summary>
public class SqliteProjectRepository : IProjectRepository
{
    private readonly ISqlSugarClient _db;

    public SqliteProjectRepository(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<Project>()
            .FirstAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Project>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<Project>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.LastAccessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetActiveProjectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<Project>()
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.LastAccessedAt)
            .FirstAsync(cancellationToken);
    }

    public async Task<Project?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<Project>()
            .Where(p => p.UserId == userId && p.Name == name)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        var count = await _db.Queryable<Project>()
            .Where(p => p.UserId == userId && p.Name == name)
            .CountAsync(cancellationToken);
        return count > 0;
    }

    public async Task ActivateProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(projectId, cancellationToken);
        if (project != null)
        {
            project.Activate();
            await _db.Updateable(project).ExecuteCommandAsync(cancellationToken);
        }
    }

    public async Task DeactivateProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(projectId, cancellationToken);
        if (project != null)
        {
            project.Deactivate();
            await _db.Updateable(project).ExecuteCommandAsync(cancellationToken);
        }
    }

    public async Task UpdateLastAccessedAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(projectId, cancellationToken);
        if (project != null)
        {
            project.UpdateLastAccessed();
            await _db.Updateable(project).ExecuteCommandAsync(cancellationToken);
        }
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _db.Insertable(project).ExecuteCommandAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _db.Updateable(project).ExecuteCommandAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<Project>().Where(p => p.Id == id).ExecuteCommandAsync(cancellationToken);
    }
}
