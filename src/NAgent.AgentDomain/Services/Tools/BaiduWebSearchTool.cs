using System.Net;
using HtmlAgilityPack;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 百度搜索爬虫工具 - 基于 HtmlAgilityPack 的原生 C# 实现
/// 直接请求百度 HTML 页面，解析搜索结果
/// </summary>
public class BaiduWebSearchTool
{
    private static readonly List<string> UserAgents = new()
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
    };

    private static readonly Random Random = new();

    /// <summary>
    /// 上次搜索时间 - 用于频率限制
    /// </summary>
    private static DateTime _lastSearchTime = DateTime.MinValue;
    private static readonly object _lockObj = new();

    /// <summary>
    /// 最小搜索间隔（秒）
    /// </summary>
    private const int MinIntervalSeconds = 5;

    /// <summary>
    /// 执行百度搜索
    /// </summary>
    public static async Task<ToolExecutionResult> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        // 1. 频率限制检查
        var waitResult = CheckRateLimit();
        if (waitResult != null)
            return waitResult;

        if (string.IsNullOrWhiteSpace(query))
            return ToolExecutionResult.Fail("搜索关键词不能为空");

        maxResults = Math.Clamp(maxResults, 1, 10);

        // 2. 执行搜索（带重试）
        var results = await ExecuteSearchWithRetryAsync(query, maxResults, cancellationToken);

        if (results == null || results.Count == 0)
            return ToolExecutionResult.Ok("未找到相关搜索结果。");

        // 3. 格式化输出
        var output = FormatSearchResults(results, query);
        return ToolExecutionResult.Ok(output, new Dictionary<string, object>
        {
            ["query"] = query,
            ["result_count"] = results.Count
        });
    }

    /// <summary>
    /// 检查频率限制
    /// </summary>
    private static ToolExecutionResult? CheckRateLimit()
    {
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
        return null;
    }

    /// <summary>
    /// 带重试的搜索执行
    /// </summary>
    private static async Task<List<SearchResult>?> ExecuteSearchWithRetryAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // 重试前等待随机时间（1-3秒）
                    var delay = Random.Next(1000, 3000);
                    await Task.Delay(delay, cancellationToken);
                }

                var results = await ExecuteSearchAsync(query, maxResults, cancellationToken);
                if (results != null && results.Count > 0)
                    return results;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        return null;
    }

    /// <summary>
    /// 执行单次搜索
    /// </summary>
    private static async Task<List<SearchResult>> ExecuteSearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var encodedQuery = System.Web.HttpUtility.UrlEncode(query);
        var url = $"https://www.baidu.com/s?wd={encodedQuery}&rn={maxResults * 2}"; // 请求更多结果以过滤广告

        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30);

        // 随机 UA
        var ua = UserAgents[Random.Next(UserAgents.Count)];
        client.DefaultRequestHeaders.Add("User-Agent", ua);
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.Add("Referer", "https://www.baidu.com/");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");

        // 手动读取字节流并检测编码，避免 GBK/GB2312 乱码
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var encoding = DetectEncoding(bytes, response.Content.Headers.ContentType?.CharSet);
        var html = encoding.GetString(bytes);

        return ParseBaiduResults(html, maxResults);
    }

    /// <summary>
    /// 解析百度搜索结果 HTML
    /// </summary>
    private static List<SearchResult> ParseBaiduResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 百度搜索结果的主要容器
        // 结果项通常在 class="result" 或 class="c-container" 的 div 中
        var resultNodes = doc.DocumentNode.SelectNodes("//div[@class='result' or @class='c-container']");

        if (resultNodes == null)
        {
            // 尝试其他选择器
            resultNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]");
        }

        if (resultNodes == null)
            return results;

        foreach (var node in resultNodes)
        {
            if (results.Count >= maxResults)
                break;

            try
            {
                var result = ExtractResultFromNode(node);
                if (result != null && IsValidResult(result))
                {
                    results.Add(result);
                }
            }
            catch { /* 跳过解析失败的项 */ }
        }

        return results;
    }

    /// <summary>
    /// 从单个结果节点提取信息
    /// </summary>
    private static SearchResult? ExtractResultFromNode(HtmlNode node)
    {
        // 跳过广告项（通常有特定的 class 或属性）
        if (IsAdvertisement(node))
            return null;

        // 提取标题
        var titleNode = node.SelectSingleNode(".//h3//a") ??
                        node.SelectSingleNode(".//a[contains(@class, 'title')]");

        if (titleNode == null)
            return null;

        var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
        var url = ExtractRealUrl(titleNode.GetAttributeValue("href", ""));

        // 提取摘要
        var snippetNode = node.SelectSingleNode(".//span[contains(@class, 'content-right')]") ??
                          node.SelectSingleNode(".//div[contains(@class, 'content-right')]//span") ??
                          node.SelectSingleNode(".//span[contains(@class, 'c-color-text')]");

        var snippet = snippetNode != null
            ? WebUtility.HtmlDecode(snippetNode.InnerText.Trim())
            : "";

        // 如果没有找到摘要，尝试其他选择器
        if (string.IsNullOrEmpty(snippet))
        {
            var abstractNode = node.SelectSingleNode(".//div[contains(@class, 'c-abstract')]");
            if (abstractNode != null)
                snippet = WebUtility.HtmlDecode(abstractNode.InnerText.Trim());
        }

        if (string.IsNullOrEmpty(title))
            return null;

        return new SearchResult
        {
            Title = title,
            Snippet = snippet,
            Url = url
        };
    }

    /// <summary>
    /// 判断是否为广告
    /// </summary>
    private static bool IsAdvertisement(HtmlNode node)
    {
        // 广告通常有以下特征：
        var classAttr = node.GetAttributeValue("class", "");

        // 1. 包含广告相关 class
        if (classAttr.Contains("ec_") ||
            classAttr.Contains("ad_") ||
            classAttr.Contains("result-op") ||
            classAttr.Contains("business") ||
            classAttr.Contains("tuijian"))
            return true;

        // 2. 包含 "广告" 字样
        var text = node.InnerText;
        if (text.Contains("广告") || text.Contains("推广"))
            return true;

        // 3. 有特定的 data- 属性
        if (node.GetAttributeValue("data-isad", "") == "1" ||
            node.GetAttributeValue("data-landurl", "") != "")
            return true;

        return false;
    }

    /// <summary>
    /// 提取真实 URL（百度使用跳转链接）
    /// </summary>
    private static string ExtractRealUrl(string href)
    {
        if (string.IsNullOrEmpty(href))
            return "";

        // 如果是百度跳转链接，提取真实 URL
        if (href.StartsWith("http://www.baidu.com/link?url=") ||
            href.StartsWith("https://www.baidu.com/link?url="))
        {
            // 百度跳转链接，返回原始链接（实际应用中可能需要进一步解析）
            return href;
        }

        return href;
    }

    /// <summary>
    /// 验证结果是否有效
    /// </summary>
    private static bool IsValidResult(SearchResult result)
    {
        // 过滤掉明显无效的结果
        if (string.IsNullOrWhiteSpace(result.Title))
            return false;

        // 标题过短可能是广告或垃圾信息
        if (result.Title.Length < 3)
            return false;

        // 检查是否为百度自己的页面
        if (result.Url.Contains("baidu.com") && !result.Url.Contains("baike.baidu.com"))
            return false;

        return true;
    }

    /// <summary>
    /// 检测 HTML 字节流的编码
    /// 百度页面实际使用 GBK/GB2312 编码，但 meta 标签可能声明为 UTF-8
    /// 通过统计中文字符比例来判断实际编码
    /// </summary>
    private static System.Text.Encoding DetectEncoding(byte[] bytes, string? headerCharset)
    {
        // 1. 优先使用 HTTP Header 中的 charset（最可靠）
        if (!string.IsNullOrWhiteSpace(headerCharset))
        {
            try
            {
                var enc = System.Text.Encoding.GetEncoding(headerCharset);
                if (enc.CodePage != 65001) // 不是 UTF-8 就直接用
                    return enc;
            }
            catch { /* 不支持的编码，继续检测 */ }
        }

        // 2. 尝试用 GBK 解码，统计有效中文字符比例
        try
        {
            var gbk = System.Text.Encoding.GetEncoding("gbk");
            var gbkText = gbk.GetString(bytes);
            var gbkValidRatio = CalculateChineseRatio(gbkText);

            var utf8Text = System.Text.Encoding.UTF8.GetString(bytes);
            var utf8ValidRatio = CalculateChineseRatio(utf8Text);

            // GBK 解码出的中文字符比例明显高于 UTF-8，说明实际是 GBK
            if (gbkValidRatio > utf8ValidRatio + 0.1)
                return gbk;
        }
        catch { /* 忽略异常 */ }

        // 3. 回退到 UTF-8
        return System.Text.Encoding.UTF8;
    }

    /// <summary>
    /// 计算字符串中有效中文字符的比例
    /// </summary>
    private static double CalculateChineseRatio(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int chineseCount = 0;
        int totalCount = 0;

        foreach (var c in text)
        {
            // 只统计汉字、字母、数字、常见标点
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
            {
                totalCount++;
                // 中文字符范围：\u4e00-\u9fff（CJK 统一表意文字）
                if (c >= '\u4e00' && c <= '\u9fff')
                    chineseCount++;
            }
        }

        return totalCount == 0 ? 0 : (double)chineseCount / totalCount;
    }

    /// <summary>
    /// 格式化搜索结果
    /// </summary>
    private static string FormatSearchResults(List<SearchResult> results, string query)
    {
        var lines = new List<string>
        {
            $"百度搜索: \"{query}\"",
            $"找到 {results.Count} 条结果：\n"
        };

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            lines.Add($"{i + 1}. {r.Title}");
            if (!string.IsNullOrEmpty(r.Snippet))
                lines.Add($"   {r.Snippet}");
            if (!string.IsNullOrEmpty(r.Url))
                lines.Add($"   链接: {r.Url}");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 搜索结果项
    /// </summary>
    private class SearchResult
    {
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
