using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// YAML 配置工具执行器 - 根据 ToolDefinition.ExecutionConfig 执行工具
/// 支持: local(本地代码映射), command(命令行), http(HTTP请求)
/// </summary>
public class YamlToolExecutor : IToolExecutor
{
    private readonly ToolDefinition _toolDefinition;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly HttpClient _httpClient;

    public string ToolName => _toolDefinition.Name;
    public string Description => _toolDefinition.Description;

    public YamlToolExecutor(
        ToolDefinition toolDefinition,
        IWorkspaceManager workspaceManager)
    {
        _toolDefinition = toolDefinition ?? throw new ArgumentNullException(nameof(toolDefinition));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(toolDefinition.ExecutionConfig.TimeoutSeconds) };
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var execType = _toolDefinition.ExecutionConfig.ExecutionType.ToLowerInvariant();

            return execType switch
            {
                "local" => await ExecuteLocalAsync(parameters, projectId, cancellationToken),
                "command" => await ExecuteCommandAsync(parameters, projectId, cancellationToken),
                "http" => await ExecuteHttpAsync(parameters, cancellationToken),
                _ => ToolExecutionResult.Fail($"不支持的执行类型: {execType}")
            };
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Fail($"工具执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行本地内置逻辑（通过工具名称映射到内置实现）
    /// </summary>
    private Task<ToolExecutionResult> ExecuteLocalAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        // 根据工具名称映射到内置逻辑
        return ToolName.ToLowerInvariant() switch
        {
            "web_search" => ExecuteWebSearchAsync(parameters, cancellationToken),
            "local_file_read" => ExecuteFileReadAsync(parameters, projectId, cancellationToken),
            "local_file_write" => ExecuteFileWriteAsync(parameters, projectId, cancellationToken),
            "list_workspace_files" => ExecuteListFilesAsync(parameters, projectId, cancellationToken),
            _ => Task.FromResult(ToolExecutionResult.Fail($"本地工具 {ToolName} 未实现"))
        };
    }

    /// <summary>
    /// Web 搜索（DuckDuckGo）
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteWebSearchAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var query = GetParameter<string>(parameters, "query");
        if (string.IsNullOrWhiteSpace(query))
            return ToolExecutionResult.Fail("搜索关键词不能为空");

        var maxResults = GetParameter<int>(parameters, "max_results", 5);
        maxResults = Math.Clamp(maxResults, 1, 10);

        // 使用 DuckDuckGo HTML 搜索
        var encodedQuery = System.Web.HttpUtility.UrlEncode(query);
        var url = $"https://html.duckduckgo.com/html/?q={encodedQuery}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.Timeout = TimeSpan.FromSeconds(30);

        var response = await client.GetStringAsync(url, cancellationToken);
        var results = ParseDuckDuckGoResults(response, maxResults);

        if (results.Count == 0)
            return ToolExecutionResult.Ok("未找到相关搜索结果。");

        var output = FormatSearchResults(results);
        return ToolExecutionResult.Ok(output, new Dictionary<string, object>
        {
            ["query"] = query,
            ["result_count"] = results.Count
        });
    }

    /// <summary>
    /// 本地文件读取
    /// </summary>
    private Task<ToolExecutionResult> ExecuteFileReadAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var relativePath = GetParameter<string>(parameters, "file_path");
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(ToolExecutionResult.Fail("文件路径不能为空"));

        // 安全检查
        if (relativePath.Contains("..") || relativePath.Contains("//") || Path.IsPathRooted(relativePath))
            return Task.FromResult(ToolExecutionResult.Fail("非法路径：只能使用相对路径"));

        // 查找项目工作空间（简化：通过遍历查找）
        var workspaceBase = FindWorkspaceBasePath();
        var projectPath = FindProjectPath(workspaceBase, projectId);
        if (projectPath == null)
            return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

        var fullPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(projectPath), StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ToolExecutionResult.Fail("非法路径：只能访问项目目录内的文件"));

        if (!File.Exists(fullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"文件不存在: {relativePath}"));

        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length > 1024 * 1024)
            return Task.FromResult(ToolExecutionResult.Fail($"文件过大，最大支持 1MB"));

        var encodingName = GetParameter<string>(parameters, "encoding", "utf-8");
        var encoding = System.Text.Encoding.GetEncoding(encodingName);
        var content = File.ReadAllText(fullPath, encoding);

        var isTruncated = content.Length > 10000;
        var displayContent = isTruncated ? content[..10000] + "\n\n... [内容已截断，共 " + content.Length + " 字符]" : content;

        return Task.FromResult(ToolExecutionResult.Ok(displayContent, new Dictionary<string, object>
        {
            ["file_path"] = relativePath,
            ["file_size"] = fileInfo.Length,
            ["char_count"] = content.Length,
            ["is_truncated"] = isTruncated
        }));
    }

    /// <summary>
    /// 本地文件写入
    /// </summary>
    private Task<ToolExecutionResult> ExecuteFileWriteAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var relativePath = GetParameter<string>(parameters, "file_path");
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.FromResult(ToolExecutionResult.Fail("文件路径不能为空"));

        var content = GetParameter<string>(parameters, "content");
        if (content == null)
            return Task.FromResult(ToolExecutionResult.Fail("文件内容不能为空"));

        // 安全检查
        if (relativePath.Contains("..") || relativePath.Contains("//") || Path.IsPathRooted(relativePath))
            return Task.FromResult(ToolExecutionResult.Fail("非法路径"));

        var workspaceBase = FindWorkspaceBasePath();
        var projectPath = FindProjectPath(workspaceBase, projectId);
        if (projectPath == null)
            return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

        var fullPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(projectPath), StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ToolExecutionResult.Fail("非法路径"));

        var mode = GetParameter<string>(parameters, "mode", "write").ToLowerInvariant();
        var createDirs = GetParameter<bool>(parameters, "create_dirs", true);

        if (createDirs)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        var isNewFile = !File.Exists(fullPath);
        if (mode == "append" && File.Exists(fullPath))
            File.AppendAllText(fullPath, content, System.Text.Encoding.UTF8);
        else
            File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);

        var fileInfo = new FileInfo(fullPath);
        var operationDesc = isNewFile ? "创建" : mode == "append" ? "追加到" : "修改";

        return Task.FromResult(ToolExecutionResult.Ok(
            $"{operationDesc}文件成功: {relativePath}",
            new Dictionary<string, object>
            {
                ["file_path"] = relativePath,
                ["operation"] = isNewFile ? "create" : mode == "append" ? "append" : "overwrite",
                ["file_size"] = fileInfo.Length
            }));
    }

    /// <summary>
    /// 遍历项目文件结构
    /// </summary>
    private Task<ToolExecutionResult> ExecuteListFilesAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var workspaceBase = FindWorkspaceBasePath();
        var projectPath = FindProjectPath(workspaceBase, projectId);
        if (projectPath == null)
            return Task.FromResult(ToolExecutionResult.Fail($"找不到项目 {projectId} 的工作空间"));

        if (!Directory.Exists(projectPath))
            return Task.FromResult(ToolExecutionResult.Ok("工作目录为空"));

        var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
            .Select(f => f.Replace(projectPath, "").TrimStart(Path.DirectorySeparatorChar))
            .OrderBy(f => f)
            .ToList();

        var output = new StringBuilder();
        output.AppendLine($"项目工作目录: {projectPath}");
        output.AppendLine($"文件总数: {files.Count}");
        output.AppendLine();
        output.AppendLine("文件列表:");
        foreach (var file in files)
        {
            output.AppendLine($"  - {file}");
        }

        return Task.FromResult(ToolExecutionResult.Ok(output.ToString(), new Dictionary<string, object>
        {
            ["file_count"] = files.Count,
            ["project_path"] = projectPath
        }));
    }

    /// <summary>
    /// 执行命令行
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteCommandAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var command = _toolDefinition.ExecutionConfig.Command;
        if (string.IsNullOrWhiteSpace(command))
            return ToolExecutionResult.Fail("命令未配置");

        // 替换参数占位符
        foreach (var param in parameters)
        {
            command = command.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _toolDefinition.ExecutionConfig.WorkingDirectory ?? Directory.GetCurrentDirectory()
        };

        // 添加环境变量
        if (_toolDefinition.ExecutionConfig.EnvironmentVariables != null)
        {
            foreach (var env in _toolDefinition.ExecutionConfig.EnvironmentVariables)
            {
                psi.EnvironmentVariables[env.Key] = env.Value;
            }
        }

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeout = _toolDefinition.ExecutionConfig.TimeoutSeconds * 1000;
        var completed = await Task.Run(() => process.WaitForExit(timeout), cancellationToken);

        if (!completed)
        {
            process.Kill();
            return ToolExecutionResult.Fail("命令执行超时");
        }

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            return ToolExecutionResult.Fail($"命令执行失败 (ExitCode: {process.ExitCode}): {error}");
        }

        return ToolExecutionResult.Ok(output, new Dictionary<string, object>
        {
            ["exit_code"] = process.ExitCode,
            ["error"] = error
        });
    }

    /// <summary>
    /// 执行 HTTP 请求
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteHttpAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var endpoint = _toolDefinition.ExecutionConfig.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            return ToolExecutionResult.Fail("HTTP 端点未配置");

        var method = _toolDefinition.ExecutionConfig.HttpMethod?.ToUpperInvariant() ?? "GET";

        // 替换参数占位符
        foreach (var param in parameters)
        {
            endpoint = endpoint.Replace($"{{{param.Key}}}", System.Web.HttpUtility.UrlEncode(param.Value?.ToString() ?? ""));
        }

        using var request = new HttpRequestMessage(
            method switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                _ => HttpMethod.Get
            },
            endpoint);

        if (method == "POST" || method == "PUT")
        {
            var json = JsonSerializer.Serialize(parameters);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ToolExecutionResult.Fail($"HTTP 请求失败: {response.StatusCode} - {content}");
        }

        return ToolExecutionResult.Ok(content, new Dictionary<string, object>
        {
            ["status_code"] = (int)response.StatusCode
        });
    }

    #region 辅助方法

    private List<SearchResult> ParseDuckDuckGoResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        var resultDivs = html.Split(new[] { "class=\"result\"" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var div in resultDivs.Skip(1).Take(maxResults))
        {
            try
            {
                var title = StripHtml(ExtractBetween(div, "class=\"result__a\"", "</a>"));
                var snippet = StripHtml(ExtractBetween(div, "class=\"result__snippet\"", "</a>"));
                var url = System.Net.WebUtility.HtmlDecode(ExtractBetween(div, "href=\"", "\""));

                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snippet))
                {
                    results.Add(new SearchResult { Title = title.Trim(), Snippet = snippet.Trim(), Url = url.Trim() });
                }
            }
            catch { }
        }

        return results;
    }

    private string FormatSearchResults(List<SearchResult> results)
    {
        var lines = new List<string> { $"找到 {results.Count} 条搜索结果：\n" };
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            lines.Add($"{i + 1}. {r.Title}");
            lines.Add($"   {r.Snippet}");
            lines.Add($"   链接: {r.Url}\n");
        }
        return string.Join("\n", lines);
    }

    private string ExtractBetween(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0) return "";
        startIndex += start.Length;
        var endIndex = source.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0) return "";
        return source[startIndex..endIndex];
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(result);
    }

    private string? FindWorkspaceBasePath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "workspace"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "workspace"),
            Path.Combine(Environment.CurrentDirectory, "workspace")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        return possiblePaths[0]; // 返回默认路径
    }

    private string? FindProjectPath(string? workspaceBase, Guid projectId)
    {
        if (string.IsNullOrEmpty(workspaceBase) || !Directory.Exists(workspaceBase))
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

    private class SearchResult
    {
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
    }

    #endregion
}
