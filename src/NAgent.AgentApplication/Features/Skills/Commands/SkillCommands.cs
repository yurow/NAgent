using MediatR;

namespace NAgent.AgentApplication.Features.Skills.Commands;

/// <summary>
/// 从目录加载 Skills 命令
/// </summary>
public record LoadSkillsFromDirectoryCommand(string DirectoryPath) : IRequest<int>;

/// <summary>
/// 重新加载所有 Skills 命令
/// </summary>
public record ReloadAllSkillsCommand : IRequest<int>;

/// <summary>
/// 启用/禁用 Skill 命令
/// </summary>
public record SetSkillEnabledCommand(Guid SkillId, bool IsEnabled) : IRequest<bool>;
