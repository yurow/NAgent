using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services;

namespace NAgent.AgentInfrastructure.Services;

/// <summary>
/// Skill 文件加载器实现
/// </summary>
public class SkillFileLoader : ISkillLoader
{
    private readonly ISkillParser _parser;
    private FileSystemWatcher? _watcher;

    public SkillFileLoader(ISkillParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>
    /// 从目录加载所有 Skills
    /// </summary>
    public async Task<IReadOnlyList<Skill>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            return new List<Skill>();

        var skills = new List<Skill>();
        var mdFiles = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);

        foreach (var file in mdFiles)
        {
            try
            {
                var skill = await LoadFromFileAsync(file, cancellationToken);
                skills.Add(skill);
            }
            catch (Exception ex)
            {
                // 记录错误但继续加载其他文件
                Console.WriteLine($"加载 Skill 文件失败 {file}: {ex.Message}");
            }
        }

        return skills;
    }

    /// <summary>
    /// 加载单个 Skill 文件
    /// </summary>
    public async Task<Skill> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Skill 文件不存在: {filePath}");

        return await _parser.ParseFromFileAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// 监视目录变化并自动重新加载
    /// </summary>
    public void WatchDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        StopWatching();

        _watcher = new FileSystemWatcher(directoryPath, "*.md")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnSkillFileChanged;
        _watcher.Created += OnSkillFileChanged;
        _watcher.Deleted += OnSkillFileChanged;
        _watcher.Renamed += OnSkillFileRenamed;

        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 停止监视
    /// </summary>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnSkillFileChanged(object sender, FileSystemEventArgs e)
    {
        // 触发重新加载事件（可通过事件总线或回调实现）
        Console.WriteLine($"Skill 文件变化: {e.FullPath}, 操作: {e.ChangeType}");
    }

    private void OnSkillFileRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"Skill 文件重命名: {e.OldFullPath} -> {e.FullPath}");
    }
}
