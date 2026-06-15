using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Tools;

/// <summary>
/// 本地文件读取工具 - 只允许读取本项目目录下的文件
/// 支持文本文件、代码文件、配置文件等
/// </summary>
public class LocalFileReadTool : IToolExecutor
{
    public string ToolName => "local_file_read";
    public string Description => "读取项目工作空间内的文件内容。参数: file_path(相对项目目录的文件路径), encoding(编码,默认utf-8)";

    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<LocalFileReadTool>? _logger;

    // 允许读取的文件扩展名白名单
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".yaml", ".yml", ".csv",
        ".cs", ".js", ".ts", ".html", ".css", ".sql", ".py", ".java",
        ".cpp", ".c", ".h", ".hpp", ".go", ".rs", ".rb", ".php",
        ".sh", ".ps1", ".bat", ".cmd", ".dockerfile", ".env",
        ".csproj", ".sln", ".cshtml", ".razor", ".vue", ".jsx", ".tsx",
        ".config", ".ini", ".properties", ".gitignore", ".editorconfig"
    };

    // 禁止读取的文件模式（敏感文件）
    private static readonly string[] ForbiddenPatterns =
    {
        "..", "//", "\\", ":", "*", "?", "<", ">", "|",
        ".exe", ".dll", ".so", ".dylib", ".bin",
        ".zip", ".rar", ".7z", ".tar", ".gz",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".svg",
        ".mp3", ".mp4", ".avi", ".mov", ".wmv",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    // 最大文件大小 1MB
    private const long MaxFileSize = 1024 * 1024;

    public LocalFileReadTool(IWorkspaceManager workspaceManager, ILogger<LocalFileReadTool>? logger = null)
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
            var relativePath = GetParameter<string>(parameters, "file_path");
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.FromResult(ToolExecutionResult.Fail("文件路径不能为空"));

            // 安全检查
            var validation = ValidatePath(relativePath);
            if (!validation.IsValid)
                return Task.FromResult(ToolExecutionResult.Fail(validation.ErrorMessage!));

            // 获取项目工作空间路径（需要 userId，这里简化处理，通过 projectId 查找）
            // 由于 IWorkspaceManager 需要 userId，我们在调用时需要传递
            // 这里使用 projectId 作为目录名来查找
            var workspacePath = FindProjectWorkspacePath(projectId);
            if (workspacePath == null)
                return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

            var fullPath = Path.GetFullPath(Path.Combine(workspacePath, relativePath));

            // 再次确认路径在项目目录内（防止路径遍历）
            if (!fullPath.StartsWith(Path.GetFullPath(workspacePath), StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ToolExecutionResult.Fail("非法路径：只能访问项目目录内的文件"));

            if (!File.Exists(fullPath))
                return Task.FromResult(ToolExecutionResult.Fail($"文件不存在: {relativePath}"));

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxFileSize)
                return Task.FromResult(ToolExecutionResult.Fail($"文件过大 ({fileInfo.Length / 1024}KB)，最大支持 {MaxFileSize / 1024}KB"));

            var encodingName = GetParameter<string>(parameters, "encoding", "utf-8");
            var encoding = System.Text.Encoding.GetEncoding(encodingName);

            var content = File.ReadAllText(fullPath, encoding);

            // 如果内容太长，截断显示
            const int maxDisplayLength = 10000;
            var isTruncated = content.Length > maxDisplayLength;
            var displayContent = isTruncated ? content[..maxDisplayLength] + "\n\n... [内容已截断，共 " + content.Length + " 字符]" : content;

            _logger?.LogInformation("LocalFileReadTool: 读取文件 {FilePath}, 大小 {Size} 字符", relativePath, content.Length);

            return Task.FromResult(ToolExecutionResult.Ok(displayContent, new Dictionary<string, object>
            {
                ["file_path"] = relativePath,
                ["full_path"] = fullPath,
                ["file_size"] = fileInfo.Length,
                ["char_count"] = content.Length,
                ["is_truncated"] = isTruncated,
                ["line_count"] = content.Split('\n').Length
            }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LocalFileReadTool 执行失败");
            return Task.FromResult(ToolExecutionResult.Fail($"读取文件失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 验证文件路径是否合法
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidatePath(string relativePath)
    {
        // 检查路径遍历攻击
        if (relativePath.Contains("..") || relativePath.Contains("//") || relativePath.Contains("\\\\"))
            return (false, "非法路径：包含路径遍历字符");

        // 检查绝对路径
        if (Path.IsPathRooted(relativePath))
            return (false, "非法路径：必须使用相对路径");

        // 检查扩展名
        var extension = Path.GetExtension(relativePath);
        if (!string.IsNullOrEmpty(extension) && !AllowedExtensions.Contains(extension))
        {
            var extLower = extension.ToLowerInvariant();
            if (ForbiddenPatterns.Any(p => extLower.Contains(p)))
                return (false, $"不支持的文件类型: {extension}。只允许读取文本和代码文件。");
        }

        return (true, null);
    }

    /// <summary>
    /// 查找项目工作空间路径
    /// </summary>
    private string? FindProjectWorkspacePath(Guid projectId)
    {
        // 遍历 workspace 目录查找包含该项目 ID 的目录
        // 由于 workspace 结构是 workspace/{userId}/{projectId}/
        var workspaceBase = GetWorkspaceBasePath();
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

    /// <summary>
    /// 获取工作空间基础路径
    /// </summary>
    private string GetWorkspaceBasePath()
    {
        // 尝试从常见位置获取
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "workspace"),
            Path.Combine(AppContext.BaseDirectory, "workspace"),
            Path.Combine(Environment.CurrentDirectory, "workspace"),
            Path.Combine(Path.GetTempPath(), "NAgent", "workspace")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        // 默认返回第一个
        return Path.GetFullPath(possiblePaths[1]);
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
            }
            catch { }
        }

        return defaultValue;
    }
}
