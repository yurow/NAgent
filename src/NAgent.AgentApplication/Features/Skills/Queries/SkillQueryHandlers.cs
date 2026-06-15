using MediatR;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentApplication.Features.Skills.Queries;

/// <summary>
/// 获取所有 Skills 查询处理器
/// </summary>
public class GetAllSkillsQueryHandler : IRequestHandler<GetAllSkillsQuery, List<SkillDto>>
{
    private readonly ISkillRepository _skillRepository;

    public GetAllSkillsQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<List<SkillDto>> Handle(GetAllSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        return skills.Select(MapToDto).ToList();
    }

    public static SkillDto MapToDto(NAgent.AgentDomain.Entities.Skill skill)
    {
        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            Category = skill.Category,
            Version = skill.Version,
            Author = skill.Author,
            IsEnabled = skill.IsEnabled,
            ToolNames = skill.ToolNames.ToList(),
            Examples = skill.Examples.Select(e => new SkillExampleDto
            {
                Title = e.Title,
                Input = e.Input,
                Output = e.Output,
                Explanation = e.Explanation
            }).ToList(),
            CreatedAt = skill.CreatedAt
        };
    }
}

/// <summary>
/// 根据分类获取 Skills 查询处理器
/// </summary>
public class GetSkillsByCategoryQueryHandler : IRequestHandler<GetSkillsByCategoryQuery, List<SkillDto>>
{
    private readonly ISkillRepository _skillRepository;

    public GetSkillsByCategoryQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<List<SkillDto>> Handle(GetSkillsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetByCategoryAsync(request.Category, cancellationToken);
        return skills.Select(GetAllSkillsQueryHandler.MapToDto).ToList();
    }
}

/// <summary>
/// 获取启用的 Skills 查询处理器
/// </summary>
public class GetEnabledSkillsQueryHandler : IRequestHandler<GetEnabledSkillsQuery, List<SkillDto>>
{
    private readonly ISkillRepository _skillRepository;

    public GetEnabledSkillsQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<List<SkillDto>> Handle(GetEnabledSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetEnabledAsync(cancellationToken);
        return skills.Select(GetAllSkillsQueryHandler.MapToDto).ToList();
    }
}

/// <summary>
/// 根据名称获取 Skill 查询处理器
/// </summary>
public class GetSkillByNameQueryHandler : IRequestHandler<GetSkillByNameQuery, SkillDto?>
{
    private readonly ISkillRepository _skillRepository;

    public GetSkillByNameQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<SkillDto?> Handle(GetSkillByNameQuery request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByNameAsync(request.Name, cancellationToken);
        return skill == null ? null : GetAllSkillsQueryHandler.MapToDto(skill);
    }
}
