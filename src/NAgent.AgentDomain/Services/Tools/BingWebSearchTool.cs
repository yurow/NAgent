using HtmlAgilityPack;
using System.Net;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// Bing 搜索爬虫工具 - 纯 C# 实现，无需 API Key
/// 直接请求 Bing HTML 页面，解析搜索结果
/// </summary>
public class BingWebSearchTool
{
    private static readonly List<string> UserAgents = new()
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0"
    };

    private static readonly Random Random = new();
    private static DateTime _lastSearchTime = DateTime.MinValue;
    private static readonly object _lockObj = new();
    private const int MinIntervalSeconds = 3;

    /// <summary>
    /// 执行 Bing 搜索
    /// </summary>
    public static async Task<ToolExecutionResult> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        // 频率限制
        lock (_lockObj)
        {
            var elapsed = DateTime.UtcNow - _lastSearchTime;
            if (elapsed.TotalSeconds < MinIntervalSeconds)
            {
                var waitSeconds = MinIntervalSeconds - (int)elapsed.TotalSeconds;
                return ToolExecutionResult.Fail($"搜索过于频繁，请等待 {waitSeconds} 秒后再试");
            }
            _lastSearchTime = DateTime.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(query))
            return ToolExecutionResult.Fail("搜索关键词不能为空");

        maxResults = Math.Clamp(maxResults, 1, 10);

        var results = await ExecuteSearchWithRetryAsync(query, maxResults, cancellationToken);

        if (results == null || results.Count == 0)
            return ToolExecutionResult.Ok("未找到相关搜索结果。");

        var output = BaiduWebSearchTool.FormatSearchResults(
            results.Select(r => new BaiduWebSearchTool.SearchResult
            {
                Title = r.Title,
                Snippet = r.Snippet,
                Url = r.Url
            }).ToList(),
            query, "Bing");

        return ToolExecutionResult.Ok(output, new Dictionary<string, object>
        {
            ["query"] = query,
            ["result_count"] = results.Count,
            ["engine"] = "bing"
        });
    }

    private static async Task<List<SearchResult>?> ExecuteSearchWithRetryAsync(
        string query, int maxResults, CancellationToken cancellationToken)
    {
        const int maxRetries = 2;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                    await Task.Delay(Random.Next(1000, 2500), cancellationToken);

                var results = await ExecuteSearchAsync(query, maxResults, cancellationToken);
                if (results.Count > 0) return results;
            }
            catch { /* retry */ }
        }
        return null;
    }

    private static async Task<List<SearchResult>> ExecuteSearchAsync(
        string query, int maxResults, CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://www.bing.com/search?q={encodedQuery}&count={maxResults * 2}&setlang=zh-Hans";

        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30);

        var ua = UserAgents[Random.Next(UserAgents.Count)];
        client.DefaultRequestHeaders.Add("User-Agent", ua);
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Bing 始终使用 UTF-8
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseBingResults(html, maxResults);
    }

    /// <summary>
    /// 解析 Bing 搜索结果
    /// </summary>
    private static List<SearchResult> ParseBingResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        var doc = new HtmlDocument();
        doc.OptionDefaultStreamEncoding = System.Text.Encoding.UTF8;
        doc.LoadHtml(html);

        // Bing 搜索结果项在 <li class="b_algo"> 中
        var resultNodes = doc.DocumentNode.SelectNodes("//li[@class='b_algo']");

        if (resultNodes == null)
        {
            // 备用选择器
            resultNodes = doc.DocumentNode.SelectNodes("//li[contains(@class, 'b_algo')]");
        }

        if (resultNodes == null) return results;

        foreach (var node in resultNodes)
        {
            if (results.Count >= maxResults) break;

            try
            {
                var result = ExtractBingResult(node);
                if (result != null)
                    results.Add(result);
            }
            catch { /* skip */ }
        }

        return results;
    }

    private static SearchResult? ExtractBingResult(HtmlNode node)
    {
        // 标题和链接
        var titleNode = node.SelectSingleNode(".//h2//a");
        if (titleNode == null) return null;

        var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
        var url = titleNode.GetAttributeValue("href", "");

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
            return null;

        // 摘要 - Bing 使用 <p> 或 <div class="b_caption">
        var snippet = "";
        var snippetNode = node.SelectSingleNode(".//p")
                       ?? node.SelectSingleNode(".//div[@class='b_caption']//p")
                       ?? node.SelectSingleNode(".//div[contains(@class, 'b_caption')]");

        if (snippetNode != null)
            snippet = WebUtility.HtmlDecode(snippetNode.InnerText.Trim());

        return new SearchResult { Title = title, Snippet = snippet, Url = url };
    }

    private class SearchResult
    {
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
