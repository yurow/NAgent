using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services;

namespace NAgent.AgentInfrastructure.Services;

/// <summary>
/// Skills 和 Tools 自动加载服务 - 应用启动时从文件系统加载
/// </summary>
public class SkillsAndToolsAutoLoader
{
    private readonly ISkillLoader _skillLoader;
    private readonly IToolLoader _toolLoader;
    private readonly ISkillRepository _skillRepository;
    private readonly IToolDefinitionRepository _toolDefinitionRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SkillsAndToolsAutoLoader> _logger;

    public SkillsAndToolsAutoLoader(
        ISkillLoader skillLoader,
        IToolLoader toolLoader,
        ISkillRepository skillRepository,
        IToolDefinitionRepository toolDefinitionRepository,
        IConfiguration configuration,
        ILogger<SkillsAndToolsAutoLoader> logger)
    {
        _skillLoader = skillLoader;
        _toolLoader = toolLoader;
        _skillRepository = skillRepository;
        _toolDefinitionRepository = toolDefinitionRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 加载 Skills 和 Tools
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // 加载 Skills
        await LoadSkillsAsync(cancellationToken);

        // 加载 Tools
        await LoadToolsAsync(cancellationToken);
    }

    private async Task LoadSkillsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var skillsDir = _configuration["Skills:Directory"];
            if (string.IsNullOrEmpty(skillsDir))
                skillsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skills");

            if (!Directory.Exists(skillsDir))
            {
                Directory.CreateDirectory(skillsDir);
                _logger.LogInformation("Skills 目录不存在，已创建: {SkillsDir}", skillsDir);
                return;
            }

            var skills = await _skillLoader.LoadFromDirectoryAsync(skillsDir, cancellationToken);
            int loadedCount = 0;

            foreach (var skill in skills)
            {
                var existing = await _skillRepository.GetByNameAsync(skill.Name, cancellationToken);
                if (existing == null)
                {
                    await _skillRepository.AddAsync(skill, cancellationToken);
                    loadedCount++;
                }
            }

            _logger.LogInformation("Skills 加载完成: 共 {TotalCount} 个, 新增 {LoadedCount} 个", skills.Count, loadedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 Skills 失败");
        }
    }

    private async Task LoadToolsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var toolsDir = _configuration["Tools:Directory"];
            if (string.IsNullOrEmpty(toolsDir))
                toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");

            if (!Directory.Exists(toolsDir))
            {
                Directory.CreateDirectory(toolsDir);
                _logger.LogInformation("Tools 目录不存在，已创建: {ToolsDir}", toolsDir);
                return;
            }

            var tools = await _toolLoader.LoadFromDirectoryAsync(toolsDir, cancellationToken);
            int loadedCount = 0;

            foreach (var tool in tools)
            {
                var existing = await _toolDefinitionRepository.GetByNameAsync(tool.Name, cancellationToken);
                if (existing == null)
                {
                    await _toolDefinitionRepository.AddAsync(tool, cancellationToken);
                    loadedCount++;
                }
            }

            _logger.LogInformation("Tools 加载完成: 共 {TotalCount} 个, 新增 {LoadedCount} 个", tools.Count, loadedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 Tools 失败");
        }
    }
}
