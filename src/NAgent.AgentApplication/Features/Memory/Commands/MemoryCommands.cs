using MediatR;
using NAgent.AgentDomain.Services.Memory;

namespace NAgent.AgentApplication.Features.Memory.Commands;

/// <summary>
/// 保存项目长期记忆命令
/// </summary>
public record SaveProjectMemoryCommand(
    Guid ProjectId,
    string Content,
    string Summary,
    int CategoryId,
    int Importance,
    Dictionary<string, object>? Metadata = null) : IRequest<Guid>;

/// <summary>
/// 清除会话记忆命令
/// </summary>
public record ClearSessionMemoryCommand(Guid ProjectId, Guid SessionId) : IRequest<bool>;

/// <summary>
/// 清除项目所有记忆命令
/// </summary>
public record ClearProjectMemoriesCommand(Guid ProjectId) : IRequest<bool>;
