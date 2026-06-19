using System.Net;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 全局 HttpClient 管理器 - 解决 HttpClient/HttpClientHandler 内存泄漏和端口耗尽问题
///
/// 核心原则：
///   1. HttpClient 全局单例复用，不随工具/请求频繁创建销毁
///   2. HttpClientHandler 绑定到 HttpClient，共享连接池（SocketsHttpHandler）
///   3. 不同配置（SSL绕过/重定向策略）使用不同的长期实例
///   4. 每个请求通过 HttpRequestMessage.Headers 设置独立请求头，避免并发竞争
///   5. 每个请求通过 CancellationToken 实现独立超时控制
///
/// 线程安全：所有 HttpClient 实例均为线程安全的，可并发使用。
/// </summary>
public static class HttpClientManager
{
    /// <summary>
    /// Web 抓取客户端 — SSL 绕过 + GZip/Deflate + 自动重定向（最多8次）
    /// 用于：WebFetch、搜索结果详情抓取
    /// </summary>
    public static HttpClient WebScraper { get; }

    /// <summary>
    /// 重定向解析客户端 — SSL 绕过 + 不跟随重定向（AllowAutoRedirect=false）
    /// 用于：百度跳转链接解析（需手动读取 Location 头）
    /// </summary>
    public static HttpClient RedirectResolver { get; }

    /// <summary>
    /// 通用客户端 — 标准配置，无 SSL 绕过
    /// 用于：LLM API 调用、YAML HTTP 工具、搜索引擎请求
    /// </summary>
    public static HttpClient Default { get; }

    static HttpClientManager()
    {
        // ====== WebScraper: 网页抓取（SSL 绕过 + 重定向） ======
        var scraperHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 8,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        WebScraper = new HttpClient(scraperHandler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // ====== RedirectResolver: 重定向解析（SSL 绕过 + 不跟随重定向） ======
        var redirectHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        RedirectResolver = new HttpClient(redirectHandler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // ====== Default: 通用客户端 ======
        var defaultHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };
        Default = new HttpClient(defaultHandler)
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    /// <summary>
    /// 创建带超时的链接 CancellationToken（用于单次请求的独立超时控制）
    ///
    /// 使用示例：
    ///   using var cts = HttpClientManager.CreateTimeoutCts(TimeSpan.FromSeconds(20), cancellationToken);
    ///   var response = await client.GetAsync(url, cts.Token);
    ///
    /// 注意：调用方负责 dispose 返回的 CancellationTokenSource
    /// </summary>
    public static CancellationTokenSource CreateTimeoutCts(
        TimeSpan timeout,
        CancellationToken linkedToken = default)
    {
        var cts = linkedToken == default
            ? new CancellationTokenSource(timeout)
            : CancellationTokenSource.CreateLinkedTokenSource(linkedToken);

        if (linkedToken != default)
            cts.CancelAfter(timeout);

        return cts;
    }

    /// <summary>
    /// 设置浏览器指纹请求头（每次请求独立设置，线程安全）
    ///
    /// 模拟真实浏览器请求，避免被反爬机制拦截。
    /// Referer 默认模拟从百度搜索进入，可按需覆盖。
    /// </summary>
    public static void SetBrowserHeaders(
        HttpRequestMessage request,
        string? userAgent = null,
        string referer = "https://www.baidu.com/")
    {
        var ua = userAgent ??
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        request.Headers.TryAddWithoutValidation("User-Agent", ua);
        request.Headers.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
        request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
        request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        request.Headers.TryAddWithoutValidation("Referer", referer);
        // 不手动添加 Accept-Encoding：HttpClientHandler.AutomaticDecompression 自动处理
    }
}
