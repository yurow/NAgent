using NAgent.Application.Interfaces;

namespace NAgent.Api.Middlewares;

/// <summary>
/// 系统初始化检查中间件
/// </summary>
public class InitializationMiddleware
{
    private readonly RequestDelegate _next;

    public InitializationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 排除以下路径，允许未初始化时访问
        var excludedPaths = new[]
        {
            "/api/initialization",
            "/api/auth/login",
            "/swagger",
            "/health",
            ".js",
            ".css",
            ".ico",
            ".png",
            ".html"
        };

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // 如果是排除的路径或静态文件，直接通过
        if (excludedPaths.Any(excluded => path.StartsWith(excluded) || path.EndsWith(excluded)))
        {
            await _next(context);
            return;
        }

        // ⭐ 从 HttpContext.RequestServices 获取 Scoped 服务
        var initializationService = context.RequestServices.GetRequiredService<IInitializationService>();

        // 检查系统是否已初始化
        var isInitialized = await initializationService.IsInitializedAsync(context.RequestAborted);

        if (!isInitialized)
        {
            // 如果未初始化且访问的不是初始化 API，重定向到初始化页面
            if (!path.StartsWith("/api/initialization"))
            {
                context.Response.Redirect("/init.html");
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// 初始化中间件扩展方法
/// </summary>
public static class InitializationMiddlewareExtensions
{
    public static IApplicationBuilder UseInitializationCheck(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InitializationMiddleware>();
    }
}
