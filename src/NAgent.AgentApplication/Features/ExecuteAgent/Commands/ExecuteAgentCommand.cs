using MediatR;

namespace NAgent.AgentApplication.Features.ExecuteAgent.Commands;

/// <summary>
/// 执行 Agent 命令
/// </summary>
public record ExecuteAgentCommand(string SessionId, string UserInput, string ProjectId, Guid UserId, string? ModelId = null) : IRequest<ExecuteAgentResult>;

/// <summary>
/// 执行 Agent 结果
/// </summary>
public record ExecuteAgentResult(
    bool Success,
    string? Output = null,
    string? ErrorMessage = null,
    string? ModelName = null,
    Dictionary<string, object>? Metadata = null);