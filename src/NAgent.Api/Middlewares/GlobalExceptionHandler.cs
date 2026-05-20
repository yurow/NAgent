using NAgent.Domain.Exceptions;
using NAgent.Shared.Exceptions;
using NAgent.Shared.Responses;

namespace NAgent.Api.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = exception switch
        {
            DomainException e => 
                ApiResponse.FailureResponse(e.Message),
            NotFoundException e => 
                ApiResponse.FailureResponse(e.Message),
            ValidationException e => 
                ApiResponse.FailureResponse(e.Message),
            _ => 
                ApiResponse.FailureResponse("服务器内部错误")
        };

        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        response.StatusCode = statusCode;

        _logger.LogError(exception, "发生未处理的异常: {Message}", exception.Message);

        await response.WriteAsJsonAsync(apiResponse);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandler>();
    }
}
