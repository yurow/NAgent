using MediatR;
using NAgent.AgentApplication.Interfaces;

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
    Dictionary<string, object>? Metadata = null)
{
    /// <summary>
    /// 调试事件列表（非流式路径使用）
    /// </summary>
    public List<DebugEvent>? DebugEvents { get; set; }
}