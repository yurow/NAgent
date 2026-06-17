using HtmlAgilityPack;
using System.Net;
using System.Text;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 百度搜索爬虫工具 - 基于 HtmlAgilityPack 的原生 C# 实现
/// </summary>
public class BaiduWebSearchTool
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

    public static async Task<ToolExecutionResult> SearchAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var waitResult = CheckRateLimit();
        if (waitResult != null) return waitResult;

        if (string.IsNullOrWhiteSpace(query))
            return ToolExecutionResult.Fail("搜索关键词不能为空");

        maxResults = Math.Clamp(maxResults, 1, 10);

        var results = await ExecuteSearchWithRetryAsync(query, maxResults, cancellationToken);

        if (results == null || results.Count == 0)
            return ToolExecutionResult.Ok("未找到相关搜索结果。");

        var output = FormatSearchResults(results, query, "百度");
        return ToolExecutionResult.Ok(output, new Dictionary<string, object>
        {
            ["query"] = query,
            ["result_count"] = results.Count,
            ["engine"] = "baidu"
        });
    }

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
        // ⭐ 显式使用 UTF-8 编码查询参数
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://www.baidu.com/s?wd={encodedQuery}&rn={maxResults * 2}&ie=utf-8";

        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30);

        var ua = UserAgents[Random.Next(UserAgents.Count)];
        client.DefaultRequestHeaders.Add("User-Agent", ua);
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        // ⭐ 不请求 br(Brotli)，因为 AutomaticDecompression 不支持 Brotli 自动解压
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        client.DefaultRequestHeaders.Add("Referer", "https://www.baidu.com/");

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        // ⭐ 使用 ReadAsStringAsync 让 HttpClient 自动处理编码
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        // 如果 ReadAsString 结果仍是乱码，尝试手动检测
        if (ContainsGarbledText(html))
        {
            var bytes = await client.GetByteArrayAsync(url, cancellationToken);
            html = DecodeWithMetaCharset(bytes) ?? Encoding.UTF8.GetString(bytes);
        }

        return ParseBaiduResults(html, maxResults);
    }

    /// <summary>
    /// 检测文本是否包含乱码特征（大量替换字符 U+FFFD）
    /// </summary>
    private static bool ContainsGarbledText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        // 取前 2000 字符检查
        var sample = text.Length > 2000 ? text[..2000] : text;
        var replacementCount = sample.Count(c => c == '\uFFFD');
        // 替换字符超过 5% 视为乱码
        return replacementCount > sample.Length * 0.05;
    }

    /// <summary>
    /// 根据 HTML meta 标签中的 charset 解码
    /// </summary>
    private static string? DecodeWithMetaCharset(byte[] bytes)
    {
        try
        {
            // 先用 ASCII 提取 meta charset 信息
            var asciiText = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
            var charsetMatch = System.Text.RegularExpressions.Regex.Match(
                asciiText, @"charset\s*=\s*[""']?\s*([a-zA-Z0-9\-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (charsetMatch.Success)
            {
                var charset = charsetMatch.Groups[1].Value.Trim().ToLowerInvariant();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc = Encoding.GetEncoding(charset);
                return enc.GetString(bytes);
            }
        }
        catch { /* 忽略 */ }
        return null;
    }

    private static List<SearchResult> ParseBaiduResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        var doc = new HtmlDocument();
        doc.OptionDefaultStreamEncoding = Encoding.UTF8;
        doc.LoadHtml(html);

        // ⭐ 百度现在使用 CSS Modules 哈希化类名（如 _content_lqis_4、title-wrapper_6E6FV）
        // 传统的 c-container、result 等类名已不存在
        // 策略：以 h3>a 作为锚点（搜索结果标题），向上找到合适的父容器作为单条结果

        var titleLinks = doc.DocumentNode.SelectNodes("//h3//a");
        if (titleLinks == null) return results;

        var processedHrefs = new HashSet<string>();

        foreach (var link in titleLinks)
        {
            if (results.Count >= maxResults) break;

            try
            {
                var title = WebUtility.HtmlDecode(link.InnerText.Trim());
                var href = link.GetAttributeValue("href", "");

                // 跳过空标题和重复链接
                if (string.IsNullOrWhiteSpace(title) || title.Length < 2) continue;
                if (!string.IsNullOrEmpty(href) && !processedHrefs.Add(href)) continue;

                // 向上找到结果容器（最近的、文本长度足够的祖先 div）
                var container = FindResultContainer(link);
                if (container == null) continue;

                // 检查是否为广告
                if (IsAdContainer(container)) continue;

                // 提取摘要
                var snippet = ExtractSnippet(container, title);

                // 提取真实 URL（百度使用跳转链接）
                var url = ExtractRealUrl(href);

                results.Add(new SearchResult { Title = title, Snippet = snippet, Url = url });
            }
            catch { /* skip */ }
        }

        return results;
    }

    /// <summary>
    /// 从标题链接向上查找结果容器
    /// 百度新版 HTML 结构：多层 div 嵌套，结果容器通常距 h3 有 3-6 层
    /// </summary>
    private static HtmlNode? FindResultContainer(HtmlNode titleLink)
    {
        var node = titleLink.ParentNode;
        // 向上遍历最多 10 层
        for (int i = 0; i < 10 && node != null; i++)
        {
            if (node.Name == "div")
            {
                var text = node.InnerText ?? "";
                // 容器文本长度 > 50 字符，说明是包含摘要等内容的结果块
                if (text.Length > 50)
                    return node;
            }
            node = node.ParentNode;
        }
        // 兜底：返回 h3 的直接父节点
        return titleLink.ParentNode?.ParentNode ?? titleLink.ParentNode;
    }

    /// <summary>
    /// 判断是否为广告容器
    /// </summary>
    private static bool IsAdContainer(HtmlNode container)
    {
        // 检查 data 属性
        foreach (var attr in container.Attributes)
        {
            if (attr.Name.StartsWith("data-") && attr.Value == "1" && attr.Name.Contains("ad"))
                return true;
        }

        // 检查 class 中的广告特征（兼容旧版 + 新版哈希类名）
        var classAttr = container.GetAttributeValue("class", "");
        if (classAttr.Contains("ec_") || classAttr.Contains("ad_") ||
            classAttr.Contains("result-op") || classAttr.Contains("business"))
            return true;

        // 检查子元素中的广告标记
        var adSpan = container.SelectSingleNode(".//span[contains(text(),'广告')] | .//span[contains(text(),'推广')]");
        if (adSpan != null) return true;

        return false;
    }

    /// <summary>
    /// 从结果容器中提取摘要文本
    /// </summary>
    private static string ExtractSnippet(HtmlNode container, string title)
    {
        // 收集容器中所有有实质文本的 span 和 p 节点
        var textNodes = container.SelectNodes(".//span | .//p | .//div");
        if (textNodes == null) return "";

        var bestText = "";
        foreach (var node in textNodes)
        {
            var text = WebUtility.HtmlDecode(node.InnerText.Trim());
            // 跳过太短的文本（通常是标签、按钮文字）
            if (text.Length < 15) continue;
            // 跳过与标题相同或包含标题的节点（避免重复）
            if (text == title) continue;
            // 选择最长的、不同于标题的文本作为摘要
            if (text.Length > bestText.Length && !text.StartsWith(title))
                bestText = text;
        }

        // 如果找到好的摘要，截断到合理长度
        if (bestText.Length > 300)
            bestText = bestText[..300] + "...";

        return bestText;
    }

    /// <summary>
    /// 提取真实 URL（百度使用跳转链接）
    /// </summary>
    private static string ExtractRealUrl(string href)
    {
        if (string.IsNullOrEmpty(href)) return "";
        // 百度跳转链接保持原样，后续可异步解析
        return href;
    }

    internal static string FormatSearchResults(List<SearchResult> results, string query, string engineName)
    {
        var lines = new List<string>
        {
            $"{engineName}搜索: \"{query}\"",
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
            // 如果抓取了详情，追加详情内容
            if (!string.IsNullOrEmpty(r.DetailContent))
            {
                lines.Add($"   详情: {r.DetailContent}");
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 抓取搜索结果链接的详情内容
    /// 支持百度跳转链接（先获取真实URL再抓取）
    /// </summary>
    public static async Task<string?> FetchUrlDetailAsync(string url, int maxLength = 1500, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // 跳过百度自有页面（搜索结果页、知道、贴吧等），但保留跳转链接和百科
        if (url.Contains("baidu.com") &&
            !url.Contains("baike.baidu.com") &&
            !url.Contains("baidu.com/link?url="))
            return null;

        try
        {
            // 如果是百度跳转链接，先获取真实URL
            var realUrl = await ResolveBaiduRedirectAsync(url, cancellationToken);
            if (string.IsNullOrEmpty(realUrl))
                return null;

            using var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);

            var ua = UserAgents[Random.Next(UserAgents.Count)];
            client.DefaultRequestHeaders.Add("User-Agent", ua);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            var response = await client.GetAsync(realUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            // 提取正文
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 移除无关元素
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style|//nav|//footer|//header|//aside|//iframe|//noscript|//advertisement") ?? new HtmlNodeCollection(null))
            {
                node.Remove();
            }

            // 优先提取 article 或 main 标签
            var mainNode = doc.DocumentNode.SelectSingleNode("//article")
                ?? doc.DocumentNode.SelectSingleNode("//main")
                ?? doc.DocumentNode.SelectSingleNode("//body");

            var text = mainNode?.InnerText ?? doc.DocumentNode.InnerText ?? "";

            // 清理文本
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            // 截断
            if (text.Length > maxLength)
                text = text[..maxLength] + "...(内容已截断)";

            return text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析百度跳转链接，获取真实URL
    /// 百度跳转链接如：http://www.baidu.com/link?url=xxx
    /// 需要跟随重定向获取真实目标URL
    /// </summary>
    private static async Task<string?> ResolveBaiduRedirectAsync(string url, CancellationToken cancellationToken)
    {
        // 如果不是百度跳转链接，直接返回
        if (!url.Contains("baidu.com/link?url="))
            return url;

        try
        {
            using var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false,
                MaxAutomaticRedirections = 1 // 最小值为1，配合 AllowAutoRedirect=false 实际不会重定向
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);

            var ua = UserAgents[Random.Next(UserAgents.Count)];
            client.DefaultRequestHeaders.Add("User-Agent", ua);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Referer", "https://www.baidu.com/s?wd=test");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");

            var response = await client.GetAsync(url, cancellationToken);

            // 301/302 重定向
            if (response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                response.StatusCode == System.Net.HttpStatusCode.Found ||
                response.StatusCode == System.Net.HttpStatusCode.SeeOther)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    // 如果是相对URL，补全为绝对URL
                    if (location.StartsWith("/"))
                    {
                        var uri = new Uri(url);
                        location = $"{uri.Scheme}://{uri.Host}{location}";
                    }
                    return location;
                }
            }

            // 如果响应成功（200），可能是直接返回了页面，返回原始URL
            if (response.IsSuccessStatusCode)
                return url;

            return null;
        }
        catch
        {
            return null;
        }
    }

    internal class SearchResult
    {
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
        public string? DetailContent { get; set; }
    }
}
