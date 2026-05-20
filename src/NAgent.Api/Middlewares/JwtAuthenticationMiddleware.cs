using NAgent.Application.Interfaces;

namespace NAgent.Api.Middlewares;

/// <summary>
/// JWT 认证中间件
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtTokenService _jwtTokenService;

    // 不需要认证的路径
    private static readonly string[] ExcludedPaths = new[]
    {
        "/api/auth/login",
        "/api/initialization",
        "/swagger",
        "/health",
        ".js",
        ".css",
        ".ico",
        ".png",
        ".html"
    };

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        IJwtTokenService jwtTokenService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 检查是否是排除的路径
        if (ExcludedPaths.Any(excluded => path.StartsWith(excluded) || path.EndsWith(excluded)))
        {
            await _next(context);
            return;
        }

        // 获取 Authorization Header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (authHeader == null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "未提供有效的认证令牌"
            });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // 验证 Token
        if (!_jwtTokenService.ValidateToken(token, out Guid userId, out string username, out bool isAdmin))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "认证令牌无效或已过期"
            });
            return;
        }

        // 将用户信息添加到 HttpContext
        context.Items["UserId"] = userId;
        context.Items["Username"] = username;
        context.Items["IsAdmin"] = isAdmin;

        await _next(context);
    }
}

/// <summary>
/// JWT 认证中间件扩展方法
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
