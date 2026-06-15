using MediatR;

namespace NAgent.AgentApplication.Features.Skills.Queries;

/// <summary>
/// 获取所有 Skills 查询
/// </summary>
public record GetAllSkillsQuery : IRequest<List<SkillDto>>;

/// <summary>
/// 根据分类获取 Skills 查询
/// </summary>
public record GetSkillsByCategoryQuery(string Category) : IRequest<List<SkillDto>>;

/// <summary>
/// 获取启用的 Skills 查询
/// </summary>
public record GetEnabledSkillsQuery : IRequest<List<SkillDto>>;

/// <summary>
/// 根据名称获取 Skill 查询
/// </summary>
public record GetSkillByNameQuery(string Name) : IRequest<SkillDto?>;

/// <summary>
/// Skill DTO
/// </summary>
public class SkillDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public List<string> ToolNames { get; set; } = new();
    public List<SkillExampleDto> Examples { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Skill 示例 DTO
/// </summary>
public class SkillExampleDto
{
    public string Title { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}
