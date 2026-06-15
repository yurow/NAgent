using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// Tool 定义仓储接口
/// </summary>
public interface IToolDefinitionRepository
{
    Task<ToolDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ToolDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolDefinition>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolDefinition>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ToolDefinition>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default);
    Task AddAsync(ToolDefinition tool, CancellationToken cancellationToken = default);
    Task UpdateAsync(ToolDefinition tool, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
