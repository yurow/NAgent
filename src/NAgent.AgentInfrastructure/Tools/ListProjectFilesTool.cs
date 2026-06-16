using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Tools;

/// <summary>
/// 项目文件结构遍历工具 - 列出项目工作空间下的所有文件和目录
/// 返回每个文件/目录的相对路径、类型和大小
/// </summary>
public class ListProjectFilesTool : IToolExecutor
{
    public string ToolName => "list_project_files";
    public string Description => "遍历项目工作空间的文件结构，返回所有文件和目录的相对路径。参数: pattern(可选的文件名过滤模式,支持通配符), include_dirs(是否包含目录,默认true), max_depth(最大遍历深度,默认10)";

    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<ListProjectFilesTool>? _logger;

    // 默认忽略的目录（通常不需要遍历的目录）
    private static readonly HashSet<string> IgnoredDirNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", ".svn", ".hg", "__pycache__",
        "bin", "obj", "Debug", "Release", ".vs", ".idea",
        "dist", "build", ".next", ".nuxt", "vendor", ".cache"
    };

    // 默认忽略的文件
    private static readonly HashSet<string> IgnoredFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".DS_Store", "Thumbs.db", "desktop.ini"
    };

    // 最大遍历深度
    private const int DefaultMaxDepth = 10;

    public ListProjectFilesTool(IWorkspaceManager workspaceManager, ILogger<ListProjectFilesTool>? logger = null)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _logger = logger;
    }

    public Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = GetParameter<string>(parameters, "pattern", "*");
            var includeDirs = GetParameter<bool>(parameters, "include_dirs", true);
            var maxDepth = GetParameter<int>(parameters, "max_depth", DefaultMaxDepth);

            // 查找项目工作空间路径
            var workspacePath = FindProjectWorkspacePath(projectId);
            if (workspacePath == null)
                return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

            if (!Directory.Exists(workspacePath))
                return Task.FromResult(ToolExecutionResult.Ok("项目工作空间目录为空，暂无文件。"));

            var entries = new List<FileEntry>();
            TraverseDirectory(workspacePath, workspacePath, entries, pattern, includeDirs, maxDepth, 0, cancellationToken);

            if (entries.Count == 0)
                return Task.FromResult(ToolExecutionResult.Ok("项目工作空间目录下未找到匹配的文件。"));

            // 构建输出
            var output = FormatOutput(entries, workspacePath);

            _logger?.LogInformation(
                "ListProjectFilesTool: 遍历项目 {ProjectId}，找到 {Count} 个条目",
                projectId, entries.Count);

            return Task.FromResult(ToolExecutionResult.Ok(output, new Dictionary<string, object>
            {
                ["total_count"] = entries.Count,
                ["file_count"] = entries.Count(e => e.IsFile),
                ["dir_count"] = entries.Count(e => !e.IsFile)
            }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ListProjectFilesTool 执行失败");
            return Task.FromResult(ToolExecutionResult.Fail($"遍历文件结构失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 递归遍历目录
    /// </summary>
    private void TraverseDirectory(
        string rootPath,
        string currentPath,
        List<FileEntry> entries,
        string pattern,
        bool includeDirs,
        int maxDepth,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if (currentDepth >= maxDepth)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // 遍历目录
            if (includeDirs)
            {
                foreach (var dir in Directory.GetDirectories(currentPath))
                {
                    var dirName = Path.GetFileName(dir);

                    // 跳过忽略的目录
                    if (IgnoredDirNames.Contains(dirName))
                        continue;

                    var relativePath = Path.GetRelativePath(rootPath, dir).Replace('\\', '/');
                    entries.Add(new FileEntry
                    {
                        RelativePath = relativePath,
                        Name = dirName,
                        IsFile = false
                    });

                    // 递归遍历子目录
                    TraverseDirectory(rootPath, dir, entries, pattern, includeDirs, maxDepth, currentDepth + 1, cancellationToken);
                }
            }

            // 遍历文件
            var searchPattern = string.IsNullOrEmpty(pattern) || pattern == "*" ? "*" : pattern;
            foreach (var file in Directory.GetFiles(currentPath, searchPattern))
            {
                var fileName = Path.GetFileName(file);

                // 跳过忽略的文件
                if (IgnoredFileNames.Contains(fileName))
                    continue;

                var relativePath = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
                var fileInfo = new FileInfo(file);

                entries.Add(new FileEntry
                {
                    RelativePath = relativePath,
                    Name = fileName,
                    IsFile = true,
                    SizeBytes = fileInfo.Length
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 跳过无权限的目录
        }
        catch (DirectoryNotFoundException)
        {
            // 目录可能已被删除
        }
    }

    /// <summary>
    /// 格式化输出（树形结构）
    /// </summary>
    private string FormatOutput(List<FileEntry> entries, string rootPath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"项目文件结构 (共 {entries.Count} 个条目):\n");

        // 按相对路径排序
        var sorted = entries.OrderBy(e => e.RelativePath).ToList();

        foreach (var entry in sorted)
        {
            var depth = entry.RelativePath.Split('/').Length - 1;
            var indent = new string(' ', depth * 2);

            if (entry.IsFile)
            {
                var sizeStr = FormatFileSize(entry.SizeBytes);
                sb.AppendLine($"{indent}📄 {entry.Name} ({sizeStr})");
            }
            else
            {
                sb.AppendLine($"{indent}📁 {entry.Name}/");
            }
        }

        // 统计摘要
        var totalFiles = entries.Count(e => e.IsFile);
        var totalDirs = entries.Count(e => !e.IsFile);
        var totalSize = entries.Where(e => e.IsFile).Sum(e => e.SizeBytes);

        sb.AppendLine($"\n---");
        sb.AppendLine($"📊 统计: {totalDirs} 个目录, {totalFiles} 个文件, 总计 {FormatFileSize(totalSize)}");

        return sb.ToString();
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    /// <summary>
    /// 查找项目工作空间路径
    /// </summary>
    private string? FindProjectWorkspacePath(Guid projectId)
    {
        var workspaceBase = _workspaceManager.GetWorkspaceBasePath();
        if (!Directory.Exists(workspaceBase))
            return null;

        foreach (var userDir in Directory.GetDirectories(workspaceBase))
        {
            var projectPath = Path.Combine(userDir, projectId.ToString());
            if (Directory.Exists(projectPath))
                return projectPath;
        }

        return null;
    }

    private static T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue = default!)
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        if (parameters.TryGetValue(key, out var rawValue) && rawValue != null)
        {
            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)rawValue.ToString()!;
                if (typeof(T) == typeof(int) && rawValue is string str)
                    return (T)(object)int.Parse(str);
                if (typeof(T) == typeof(bool) && rawValue is string boolStr)
                    return (T)(object)bool.Parse(boolStr);
                if (typeof(T) == typeof(bool) && rawValue is bool b)
                    return (T)(object)b;
            }
            catch { }
        }

        return defaultValue;
    }

    private class FileEntry
    {
        public string RelativePath { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsFile { get; set; }
        public long SizeBytes { get; set; }
    }
}
