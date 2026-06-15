using MediatR;
using NAgent.AgentApplication.Features.Memory.Commands;
using NAgent.AgentDomain.Services.Memory;

namespace NAgent.AgentApplication.Features.Memory.Commands;

/// <summary>
/// 保存项目长期记忆命令处理器
/// </summary>
public class SaveProjectMemoryCommandHandler : IRequestHandler<SaveProjectMemoryCommand, Guid>
{
    private readonly IMemorySystem _memorySystem;

    public SaveProjectMemoryCommandHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<Guid> Handle(SaveProjectMemoryCommand request, CancellationToken cancellationToken)
    {
        await _memorySystem.SaveProjectLongTermMemoryAsync(
            request.ProjectId,
            request.Content,
            request.Summary,
            request.CategoryId,
            request.Importance,
            request.Metadata,
            cancellationToken);

        return Guid.NewGuid(); // 返回操作 ID
    }
}

/// <summary>
/// 清除会话记忆命令处理器
/// </summary>
public class ClearSessionMemoryCommandHandler : IRequestHandler<ClearSessionMemoryCommand, bool>
{
    private readonly IMemorySystem _memorySystem;

    public ClearSessionMemoryCommandHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<bool> Handle(ClearSessionMemoryCommand request, CancellationToken cancellationToken)
    {
        await _memorySystem.ClearSessionMemoryAsync(request.ProjectId, request.SessionId, cancellationToken);
        return true;
    }
}

/// <summary>
/// 清除项目所有记忆命令处理器
/// </summary>
public class ClearProjectMemoriesCommandHandler : IRequestHandler<ClearProjectMemoriesCommand, bool>
{
    private readonly IMemorySystem _memorySystem;

    public ClearProjectMemoriesCommandHandler(IMemorySystem memorySystem)
    {
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
    }

    public async Task<bool> Handle(ClearProjectMemoriesCommand request, CancellationToken cancellationToken)
    {
        await _memorySystem.ClearProjectAllMemoriesAsync(request.ProjectId, cancellationToken);
        return true;
    }
}
