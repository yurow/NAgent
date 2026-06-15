using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.Application.Features.Auth.Queries;
using NAgent.Application.Interfaces;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IMediator mediator,
        IJwtTokenService jwtTokenService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var query = new LoginQuery(request.Username, request.Password);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse.FailureResponse(result.ErrorMessage ?? "登录失败"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            Token = result.Token,
            User = result.User,
            ExpiresIn = 3600 // 1小时
        }));
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(ApiResponse.FailureResponse("未提供有效的 Token"));
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        if (_jwtTokenService.ValidateToken(token, out Guid userId, out string username, out bool isAdmin))
        {
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                IsValid = true,
                UserId = userId,
                Username = username,
                IsAdmin = isAdmin
            }));
        }

        return Unauthorized(ApiResponse.FailureResponse("Token 无效或已过期"));
    }
}

/// <summary>
/// 登录请求 DTO
/// </summary>
public record LoginRequest(
    string Username,
    string Password);
