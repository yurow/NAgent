using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services;

namespace NAgent.AgentInfrastructure.Services;

/// <summary>
/// Tool 文件加载器实现
/// </summary>
public class ToolFileLoader : IToolLoader
{
    private readonly IToolDefinitionParser _parser;
    private FileSystemWatcher? _watcher;

    public ToolFileLoader(IToolDefinitionParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>
    /// 从目录加载所有 Tools
    /// </summary>
    public async Task<IReadOnlyList<ToolDefinition>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            return new List<ToolDefinition>();

        var tools = new List<ToolDefinition>();
        var yamlFiles = Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directoryPath, "*.yml", SearchOption.AllDirectories));

        foreach (var file in yamlFiles)
        {
            try
            {
                var tool = await LoadFromFileAsync(file, cancellationToken);
                tools.Add(tool);
            }
            catch (Exception ex)
            {
                // 记录错误但继续加载其他文件
                Console.WriteLine($"加载 Tool 文件失败 {file}: {ex.Message}");
            }
        }

        return tools;
    }

    /// <summary>
    /// 加载单个 Tool 文件
    /// </summary>
    public async Task<ToolDefinition> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Tool 文件不存在: {filePath}");

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

        _watcher = new FileSystemWatcher(directoryPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        // 监视 yaml 和 yml 文件
        _watcher.Filter = "*.yaml";
        _watcher.Changed += OnToolFileChanged;
        _watcher.Created += OnToolFileChanged;
        _watcher.Deleted += OnToolFileChanged;
        _watcher.Renamed += OnToolFileRenamed;

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

    private void OnToolFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".yaml") || e.FullPath.EndsWith(".yml"))
        {
            Console.WriteLine($"Tool 文件变化: {e.FullPath}, 操作: {e.ChangeType}");
        }
    }

    private void OnToolFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.EndsWith(".yaml") || e.FullPath.EndsWith(".yml"))
        {
            Console.WriteLine($"Tool 文件重命名: {e.OldFullPath} -> {e.FullPath}");
        }
    }
}
