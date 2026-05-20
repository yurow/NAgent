using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// Agent 工具仓储实现（内存存储）
/// </summary>
public class InMemoryAgentToolRepository : IAgentToolRepository
{
    private readonly Dictionary<Guid, AgentTool> _tools = new();

    public Task<AgentTool?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tools.TryGetValue(id, out var tool);
        return Task.FromResult(tool);
    }

    public Task<AgentTool?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var tool = _tools.Values.FirstOrDefault(t => t.Name == name);
        return Task.FromResult(tool);
    }

    public Task<List<AgentTool>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var enabledTools = _tools.Values.Where(t => t.IsEnabled).ToList();
        return Task.FromResult(enabledTools);
    }

    public Task AddAsync(AgentTool tool, CancellationToken cancellationToken = default)
    {
        _tools[tool.Id] = tool;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AgentTool tool, CancellationToken cancellationToken = default)
    {
        _tools[tool.Id] = tool;
        return Task.CompletedTask;
    }
}
