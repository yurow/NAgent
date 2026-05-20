using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.Application.Interfaces;
using NAgent.Domain.Repositories;
using NAgent.Shared.Responses;
using System.Security.Cryptography;
using System.Text;

namespace NAgent.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(ApiResponse.FailureResponse("用户名不能为空"));

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResponse.FailureResponse("密码不能为空"));

            // 查找用户
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            
            if (user == null)
            {
                return Unauthorized(ApiResponse.FailureResponse("用户名或密码错误"));
            }

            // 验证密码
            var passwordHash = HashPassword(request.Password);
            if (user.PasswordHash != passwordHash)
            {
                return Unauthorized(ApiResponse.FailureResponse("用户名或密码错误"));
            }

            // 检查账号是否激活
            if (!user.IsActive)
            {
                return Unauthorized(ApiResponse.FailureResponse("账号已被禁用"));
            }

            // 生成 JWT Token
            var token = _jwtTokenService.GenerateToken(user.Id, user.Username, user.IsAdmin);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.IsAdmin
                },
                ExpiresIn = 3600 // 1小时
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.FailureResponse($"登录失败: {ex.Message}"));
        }
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

    /// <summary>
    /// 哈希密码（与初始化服务保持一致）
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

/// <summary>
/// 登录请求 DTO
/// </summary>
public record LoginRequest(
    string Username,
    string Password);