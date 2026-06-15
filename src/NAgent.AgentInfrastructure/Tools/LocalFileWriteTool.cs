using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Tools;

/// <summary>
/// 本地文件创建/修改工具 - 只允许在本项目目录下创建或修改文件
/// 支持创建新文件、覆盖写入、追加内容
/// </summary>
public class LocalFileWriteTool : IToolExecutor
{
    public string ToolName => "local_file_write";
    public string Description => "在项目工作空间内创建或修改文件。参数: file_path(相对项目目录的文件路径), content(文件内容), mode(写入模式: write/append,默认write), create_dirs(是否自动创建目录,默认true)";

    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<LocalFileWriteTool>? _logger;

    // 允许写入的文件扩展名白名单
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".yaml", ".yml", ".csv",
        ".cs", ".js", ".ts", ".html", ".css", ".sql", ".py", ".java",
        ".cpp", ".c", ".h", ".hpp", ".go", ".rs", ".rb", ".php",
        ".sh", ".ps1", ".bat", ".cmd", ".dockerfile", ".env",
        ".csproj", ".sln", ".cshtml", ".razor", ".vue", ".jsx", ".tsx",
        ".config", ".ini", ".properties", ".gitignore", ".editorconfig"
    };

    // 禁止写入的文件模式
    private static readonly string[] ForbiddenPatterns =
    {
        "..", "//", "\\", ":", "*", "?", "<", ">", "|"
    };

    // 最大文件大小 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    public LocalFileWriteTool(IWorkspaceManager workspaceManager, ILogger<LocalFileWriteTool>? logger = null)
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

            var content = GetParameter<string>(parameters, "content");
            if (content == null)
                return Task.FromResult(ToolExecutionResult.Fail("文件内容不能为空"));

            // 安全检查
            var validation = ValidatePath(relativePath);
            if (!validation.IsValid)
                return Task.FromResult(ToolExecutionResult.Fail(validation.ErrorMessage!));

            // 获取项目工作空间路径
            var workspacePath = FindProjectWorkspacePath(projectId);
            if (workspacePath == null)
                return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

            var fullPath = Path.GetFullPath(Path.Combine(workspacePath, relativePath));

            // 再次确认路径在项目目录内（防止路径遍历）
            if (!fullPath.StartsWith(Path.GetFullPath(workspacePath), StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ToolExecutionResult.Fail("非法路径：只能访问项目目录内的文件"));

            // 检查内容大小
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
            if (contentBytes.Length > MaxFileSize)
                return Task.FromResult(ToolExecutionResult.Fail($"内容过大 ({contentBytes.Length / 1024}KB)，最大支持 {MaxFileSize / 1024}KB"));

            // 获取写入模式
            var mode = GetParameter<string>(parameters, "mode", "write").ToLowerInvariant();
            var createDirs = GetParameter<bool>(parameters, "create_dirs", true);

            // 自动创建目录
            if (createDirs)
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // 写入文件
            var isNewFile = !File.Exists(fullPath);
            var originalContent = "";

            if (mode == "append" && File.Exists(fullPath))
            {
                originalContent = File.ReadAllText(fullPath);
                File.AppendAllText(fullPath, content, System.Text.Encoding.UTF8);
            }
            else
            {
                if (!isNewFile)
                    originalContent = File.ReadAllText(fullPath);

                File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
            }

            var fileInfo = new FileInfo(fullPath);

            _logger?.LogInformation(
                "LocalFileWriteTool: {Operation} 文件 {FilePath}, 大小 {Size} 字节",
                isNewFile ? "创建" : mode == "append" ? "追加" : "修改",
                relativePath,
                fileInfo.Length);

            var operationDesc = isNewFile ? "创建" : mode == "append" ? "追加到" : "修改";

            return Task.FromResult(ToolExecutionResult.Ok(
                $"{operationDesc}文件成功: {relativePath}\n" +
                $"文件大小: {fileInfo.Length} 字节\n" +
                $"字符数: {content.Length}",
                new Dictionary<string, object>
                {
                    ["file_path"] = relativePath,
                    ["full_path"] = fullPath,
                    ["operation"] = isNewFile ? "create" : mode == "append" ? "append" : "overwrite",
                    ["is_new_file"] = isNewFile,
                    ["file_size"] = fileInfo.Length,
                    ["char_count"] = content.Length
                }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LocalFileWriteTool 执行失败");
            return Task.FromResult(ToolExecutionResult.Fail($"写入文件失败: {ex.Message}"));
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

        // 检查禁止字符
        foreach (var pattern in ForbiddenPatterns)
        {
            if (relativePath.Contains(pattern))
                return (false, $"非法路径：包含禁止字符 '{pattern}'");
        }

        // 检查扩展名
        var extension = Path.GetExtension(relativePath);
        if (!string.IsNullOrEmpty(extension) && !AllowedExtensions.Contains(extension))
            return (false, $"不支持的文件类型: {extension}。只允许创建文本和代码文件。");

        // 检查文件名是否合法
        var fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.StartsWith(".") && fileName.Length <= 1)
            return (false, "非法文件名");

        return (true, null);
    }

    /// <summary>
    /// 查找项目工作空间路径
    /// </summary>
    private string? FindProjectWorkspacePath(Guid projectId)
    {
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
}
