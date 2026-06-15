using MediatR;
using Microsoft.Extensions.Configuration;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services;

namespace NAgent.AgentApplication.Features.Skills.Commands;

/// <summary>
/// 从目录加载 Skills 命令处理器
/// </summary>
public class LoadSkillsFromDirectoryCommandHandler : IRequestHandler<LoadSkillsFromDirectoryCommand, int>
{
    private readonly ISkillLoader _skillLoader;
    private readonly ISkillRepository _skillRepository;

    public LoadSkillsFromDirectoryCommandHandler(
        ISkillLoader skillLoader,
        ISkillRepository skillRepository)
    {
        _skillLoader = skillLoader ?? throw new ArgumentNullException(nameof(skillLoader));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<int> Handle(LoadSkillsFromDirectoryCommand request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.DirectoryPath))
            throw new DirectoryNotFoundException($"Skills 目录不存在: {request.DirectoryPath}");

        var skills = await _skillLoader.LoadFromDirectoryAsync(request.DirectoryPath, cancellationToken);

        int loadedCount = 0;
        foreach (var skill in skills)
        {
            var existing = await _skillRepository.GetByNameAsync(skill.Name, cancellationToken);
            if (existing == null)
            {
                await _skillRepository.AddAsync(skill, cancellationToken);
                loadedCount++;
            }
            else
            {
                existing.UpdateContent(skill.MarkdownContent);
                await _skillRepository.UpdateAsync(existing, cancellationToken);
                loadedCount++;
            }
        }

        return loadedCount;
    }
}

/// <summary>
/// 重新加载所有 Skills 命令处理器
/// </summary>
public class ReloadAllSkillsCommandHandler : IRequestHandler<ReloadAllSkillsCommand, int>
{
    private readonly ISkillLoader _skillLoader;
    private readonly ISkillRepository _skillRepository;
    private readonly string _skillsDirectory;

    public ReloadAllSkillsCommandHandler(
        ISkillLoader skillLoader,
        ISkillRepository skillRepository,
        IConfiguration configuration)
    {
        _skillLoader = skillLoader ?? throw new ArgumentNullException(nameof(skillLoader));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _skillsDirectory = configuration["Skills:Directory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skills");
    }

    public async Task<int> Handle(ReloadAllSkillsCommand request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_skillsDirectory))
            return 0;

        var skills = await _skillLoader.LoadFromDirectoryAsync(_skillsDirectory, cancellationToken);

        int loadedCount = 0;
        foreach (var skill in skills)
        {
            var existing = await _skillRepository.GetByNameAsync(skill.Name, cancellationToken);
            if (existing == null)
            {
                await _skillRepository.AddAsync(skill, cancellationToken);
                loadedCount++;
            }
            else
            {
                existing.UpdateContent(skill.MarkdownContent);
                await _skillRepository.UpdateAsync(existing, cancellationToken);
                loadedCount++;
            }
        }

        return loadedCount;
    }
}

/// <summary>
/// 启用/禁用 Skill 命令处理器
/// </summary>
public class SetSkillEnabledCommandHandler : IRequestHandler<SetSkillEnabledCommand, bool>
{
    private readonly ISkillRepository _skillRepository;

    public SetSkillEnabledCommandHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    public async Task<bool> Handle(SetSkillEnabledCommand request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(request.SkillId, cancellationToken);
        if (skill == null)
            return false;

        skill.SetEnabled(request.IsEnabled);
        await _skillRepository.UpdateAsync(skill, cancellationToken);
        return true;
    }
}
