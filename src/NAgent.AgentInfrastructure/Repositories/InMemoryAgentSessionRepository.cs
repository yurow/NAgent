using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentInfrastructure.Repositories;

/// <summary>
/// Agent 会话仓储实现（内存存储）
/// </summary>
public class InMemoryAgentSessionRepository : IAgentSessionRepository
{
    private readonly Dictionary<Guid, AgentSession> _sessions = new();

    public Task<AgentSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<AgentSession?> GetBySessionKeyAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.SessionKey == sessionKey);
        return Task.FromResult(session);
    }

    public Task AddAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sessions.Remove(id);
        return Task.CompletedTask;
    }
}
