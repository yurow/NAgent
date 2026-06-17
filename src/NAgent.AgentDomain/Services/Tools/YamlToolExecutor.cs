using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger _logger;

    public string ToolName => _toolDefinition.Name;
    public string Description => _toolDefinition.Description;

    public YamlToolExecutor(
        ToolDefinition toolDefinition,
        IWorkspaceManager workspaceManager,
        ILogger? logger = null)
    {
        _toolDefinition = toolDefinition ?? throw new ArgumentNullException(nameof(toolDefinition));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(toolDefinition.ExecutionConfig.TimeoutSeconds) };
        _logger = logger ?? NullLogger.Instance;
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
            "web_fetch" => ExecuteWebFetchAsync(parameters, cancellationToken),
            "local_file_read" => ExecuteFileReadAsync(parameters, projectId, cancellationToken),
            "local_file_write" => ExecuteFileWriteAsync(parameters, projectId, cancellationToken),
            "list_workspace_files" => ExecuteListFilesAsync(parameters, projectId, cancellationToken),
            _ => Task.FromResult(ToolExecutionResult.Fail($"本地工具 {ToolName} 未实现"))
        };
    }

    /// <summary>
    /// Web 搜索 - 多引擎 Fallback（百度优先，Bing 备用）
    /// 搜索成功后自动抓取前3个结果的详情内容
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteWebSearchAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var query = GetParameter<string>(parameters, "query");
        var maxResults = GetParameter<int>(parameters, "max_results", 10);
        var fetchDetails = GetParameter<bool>(parameters, "fetch_details", true);

        _logger.LogInformation("[WebSearch] 查询词: {Query}, 最大结果数: {MaxResults}, 抓取详情: {FetchDetails}", query, maxResults, fetchDetails);

        // 1. 先尝试百度搜索
        _logger.LogDebug("[WebSearch] 尝试百度搜索...");
        var baiduResult = await BaiduWebSearchTool.SearchAsync(query, maxResults, cancellationToken);
        if (baiduResult.Success && !baiduResult.Output.Contains("未找到相关搜索结果"))
        {
            _logger.LogInformation("[WebSearch] 百度搜索成功，结果数: {Count}",
                baiduResult.Metadata?.GetValueOrDefault("result_count", 0));

            // 自动抓取详情
            if (fetchDetails)
            {
                await FetchDetailsForResultsAsync(baiduResult, cancellationToken);
            }

            return baiduResult;
        }
        _logger.LogDebug("[WebSearch] 百度未返回有效结果，切换到 Bing");

        // 2. 百度失败，尝试 Bing
        var bingResult = await BingWebSearchTool.SearchAsync(query, maxResults, cancellationToken);
        if (bingResult.Success && !bingResult.Output.Contains("未找到相关搜索结果"))
        {
            _logger.LogInformation("[WebSearch] Bing 搜索成功，结果数: {Count}",
                bingResult.Metadata?.GetValueOrDefault("result_count", 0));

            // 自动抓取详情
            if (fetchDetails)
            {
                await FetchDetailsForResultsAsync(bingResult, cancellationToken);
            }

            return bingResult;
        }

        // 3. 都失败，返回百度结果（即使是空的）
        _logger.LogWarning("[WebSearch] 所有搜索引擎均未返回有效结果，查询词: {Query}", query);
        return baiduResult;
    }

    /// <summary>
    /// 为搜索结果抓取详情内容（前3个结果）
    /// </summary>
    private async Task FetchDetailsForResultsAsync(ToolExecutionResult searchResult, CancellationToken cancellationToken)
    {
        try
        {
            // 从输出文本中提取链接
            var urls = ExtractUrlsFromSearchOutput(searchResult.Output);
            if (urls.Count == 0) return;

            _logger.LogInformation("[WebSearch] 开始抓取 {Count} 个链接的详情...", Math.Min(urls.Count, 3));

            var fetchTasks = urls.Take(3).Select(async url =>
            {
                try
                {
                    var detail = await BaiduWebSearchTool.FetchUrlDetailAsync(url, 1500, cancellationToken);
                    return new { Url = url, Detail = detail };
                }
                catch
                {
                    return new { Url = url, Detail = (string?)null };
                }
            });

            var details = await Task.WhenAll(fetchTasks);

            // 将详情追加到输出中
            var detailLines = new List<string>();
            foreach (var d in details)
            {
                if (!string.IsNullOrEmpty(d.Detail))
                {
                    detailLines.Add($"\n【详情: {d.Url}】\n{d.Detail}");
                }
            }

            if (detailLines.Count > 0)
            {
                searchResult.Output += "\n\n=== 搜索结果详情 ===" + string.Join("", detailLines);
                _logger.LogInformation("[WebSearch] 成功抓取 {Count} 个链接的详情", detailLines.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[WebSearch] 抓取详情时出错: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// 从搜索输出中提取 URL 链接
    /// </summary>
    private List<string> ExtractUrlsFromSearchOutput(string output)
    {
        var urls = new List<string>();
        if (string.IsNullOrWhiteSpace(output)) return urls;

        // 匹配 "链接: http://..." 或 "链接: https://..."
        var matches = System.Text.RegularExpressions.Regex.Matches(output, @"链接:\s*(https?://[^\s]+)");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var url = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(url) && !urls.Contains(url))
                urls.Add(url);
        }

        return urls;
    }

    /// <summary>
    /// Web Fetch - 抓取指定 URL 的网页正文内容
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteWebFetchAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var url = GetParameter<string>(parameters, "url");
        if (string.IsNullOrWhiteSpace(url))
            return ToolExecutionResult.Fail("缺少必填参数: url");

        var maxLength = GetParameter<int>(parameters, "max_length", 3000);

        _logger.LogInformation("[WebFetch] 抓取 URL: {Url}, 最大长度: {MaxLength}", url, maxLength);

        try
        {
            using var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,*/*;q=0.8");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogWarning("[WebFetch] HTTP {StatusCode} 抓取失败: {Url}", statusCode, url);
                return ToolExecutionResult.Fail($"HTTP {statusCode} 抓取失败");
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
                return ToolExecutionResult.Fail("页面内容为空");

            // 用 HtmlAgilityPack 提取正文
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // 移除无关元素
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style|//nav|//footer|//header|//aside|//iframe|//noscript") ?? new HtmlAgilityPack.HtmlNodeCollection(null))
            {
                node.Remove();
            }

            // 尝试提取标题
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var title = titleNode?.InnerText?.Trim() ?? "";

            // 尝试提取 article 或 main 标签
            var mainNode = doc.DocumentNode.SelectSingleNode("//article") 
                ?? doc.DocumentNode.SelectSingleNode("//main")
                ?? doc.DocumentNode.SelectSingleNode("//body");

            var text = mainNode?.InnerText ?? doc.DocumentNode.InnerText ?? "";

            // 清理文本：去除多余空白
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            // 截断
            if (text.Length > maxLength)
                text = text[..maxLength] + "...(内容已截断)";

            var output = $"标题: {title}\n来源: {url}\n\n{text}";

            _logger.LogInformation("[WebFetch] 抓取成功，文本长度: {Length}", text.Length);
            return ToolExecutionResult.Ok(output);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[WebFetch] 抓取超时: {Url}", url);
            return ToolExecutionResult.Fail("抓取超时（15秒）");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[WebFetch] 抓取异常: {Url}, {Message}", url, ex.Message);
            return ToolExecutionResult.Fail($"抓取失败: {ex.Message}");
        }
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

    private string? FindWorkspaceBasePath()
    {
        // 优先使用 IWorkspaceManager 提供的基础路径
        var managerPath = _workspaceManager.GetWorkspaceBasePath();
        if (!string.IsNullOrEmpty(managerPath) && Directory.Exists(managerPath))
            return managerPath;

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

    #endregion
}