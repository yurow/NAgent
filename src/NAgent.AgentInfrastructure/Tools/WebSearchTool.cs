using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Tools;

/// <summary>
/// Web 搜索工具 - 使用免费的 DuckDuckGo 搜索 API
/// 支持频率限制，避免被限流
/// </summary>
public class WebSearchTool : IToolExecutor
{
    public string ToolName => "web_search";
    public string Description => "使用 DuckDuckGo 进行网络搜索，获取实时信息。参数: query(搜索关键词), max_results(最大结果数,默认5)";

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly TimeSpan _minInterval;
    private readonly ILogger<WebSearchTool>? _logger;

    /// <summary>
    /// 最小请求间隔（默认 2 秒，避免限流）
    /// </summary>
    public WebSearchTool(TimeSpan? minInterval = null, ILogger<WebSearchTool>? logger = null)
    {
        _minInterval = minInterval ?? TimeSpan.FromSeconds(2);
        _rateLimiter = new SemaphoreSlim(1, 1);
        _logger = logger;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 频率限制
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var elapsed = DateTime.UtcNow - _lastRequestTime;
                if (elapsed < _minInterval)
                {
                    var delay = _minInterval - elapsed;
                    _logger?.LogInformation("WebSearchTool 频率限制: 等待 {DelayMs}ms", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }

                var query = GetParameter<string>(parameters, "query");
                if (string.IsNullOrWhiteSpace(query))
                    return ToolExecutionResult.Fail("搜索关键词不能为空");

                var maxResults = GetParameter<int>(parameters, "max_results", 5);
                maxResults = Math.Clamp(maxResults, 1, 10);

                _lastRequestTime = DateTime.UtcNow;

                var results = await SearchDuckDuckGoAsync(query, maxResults, cancellationToken);

                if (results.Count == 0)
                    return ToolExecutionResult.Ok("未找到相关搜索结果。");

                var output = FormatResults(results);
                return ToolExecutionResult.Ok(output, new Dictionary<string, object>
                {
                    ["query"] = query,
                    ["result_count"] = results.Count,
                    ["source"] = "duckduckgo"
                });
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "WebSearchTool 执行失败");
            return ToolExecutionResult.Fail($"搜索失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用 DuckDuckGo HTML 搜索（免费，无需 API Key）
    /// </summary>
    private async Task<List<SearchResult>> SearchDuckDuckGoAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"https://html.duckduckgo.com/html/?q={encodedQuery}";

        var response = await _httpClient.GetStringAsync(url, cancellationToken);

        // 解析 HTML 结果（DuckDuckGo HTML 版本的简单解析）
        var doc = new System.Xml.XmlDocument();
        try
        {
            // 使用简单的字符串解析提取结果
            results = ParseDuckDuckGoHtml(response, maxResults);
        }
        catch
        {
            // 如果解析失败，尝试备用方法
            results = ParseDuckDuckGoSimple(response, maxResults);
        }

        return results;
    }

    private List<SearchResult> ParseDuckDuckGoHtml(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        // DuckDuckGo HTML 结果在 .result 类中
        var resultDivs = html.Split(new[] { "class=\"result\"" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var div in resultDivs.Skip(1).Take(maxResults))
        {
            try
            {
                var title = ExtractBetween(div, "class=\"result__a\"", "</a>");
                title = StripHtml(title);

                var snippet = ExtractBetween(div, "class=\"result__snippet\"", "</a>");
                snippet = StripHtml(snippet);

                var url = ExtractBetween(div, "href=\"", "\"");
                url = System.Net.WebUtility.HtmlDecode(url);

                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snippet))
                {
                    results.Add(new SearchResult
                    {
                        Title = title.Trim(),
                        Snippet = snippet.Trim(),
                        Url = url.Trim()
                    });
                }
            }
            catch { /* 忽略单个解析错误 */ }
        }

        return results;
    }

    private List<SearchResult> ParseDuckDuckGoSimple(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        // 备用解析：查找所有链接和文本块
        var lines = html.Split('\n');
        string? currentTitle = null;
        string? currentUrl = null;

        foreach (var line in lines)
        {
            if (line.Contains("class=\"result__a\""))
            {
                currentTitle = StripHtml(line);
                currentUrl = ExtractBetween(line, "href=\"", "\"");
            }
            else if (line.Contains("class=\"result__snippet\"") && currentTitle != null)
            {
                var snippet = StripHtml(line);
                if (!string.IsNullOrWhiteSpace(snippet))
                {
                    results.Add(new SearchResult
                    {
                        Title = currentTitle.Trim(),
                        Snippet = snippet.Trim(),
                        Url = currentUrl ?? ""
                    });

                    if (results.Count >= maxResults) break;
                }

                currentTitle = null;
                currentUrl = null;
            }
        }

        return results;
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

        // 移除 HTML 标签
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        // 解码 HTML 实体
        result = System.Net.WebUtility.HtmlDecode(result);
        return result;
    }

    private string FormatResults(List<SearchResult> results)
    {
        var lines = new List<string>();
        lines.Add($"找到 {results.Count} 条搜索结果：\n");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            lines.Add($"{i + 1}. {r.Title}");
            lines.Add($"   {r.Snippet}");
            lines.Add($"   链接: {r.Url}\n");
        }

        return string.Join("\n", lines);
    }

    private static T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue = default!)
    {
        if (parameters.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        // 尝试转换
        if (parameters.TryGetValue(key, out var rawValue) && rawValue != null)
        {
            try
            {
                if (typeof(T) == typeof(int) && rawValue is string str)
                    return (T)(object)int.Parse(str);
                if (typeof(T) == typeof(string))
                    return (T)(object)rawValue.ToString()!;
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
}
