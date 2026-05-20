using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.Application.Interfaces;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 系统初始化控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("System Initialization")]
public class InitializationController : ControllerBase
{
    private readonly IInitializationService _initializationService;

    public InitializationController(IInitializationService initializationService)
    {
        _initializationService = initializationService ?? throw new ArgumentNullException(nameof(initializationService));
    }

    /// <summary>
    /// 检查系统初始化状态
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var isInitialized = await _initializationService.IsInitializedAsync(cancellationToken);
        
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            IsInitialized = isInitialized,
            Message = isInitialized ? "系统已初始化" : "系统未初始化，请创建管理员账号"
        }));
    }

    /// <summary>
    /// 执行系统初始化（创建管理员账号）
    /// </summary>
    [HttpPost("initialize")]
    [AllowAnonymous]
    public async Task<IActionResult> Initialize([FromBody] InitializeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(ApiResponse.FailureResponse("用户名不能为空"));

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(ApiResponse.FailureResponse("邮箱不能为空"));

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return BadRequest(ApiResponse.FailureResponse("密码长度至少为6个字符"));

            // 执行初始化
            await _initializationService.InitializeAsync(
                request.Username,
                request.Email,
                request.Password,
                cancellationToken
            );

            return Ok(ApiResponse.SuccessResponse("系统初始化成功，管理员账号已创建"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.FailureResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.FailureResponse($"初始化失败: {ex.Message}"));
        }
    }
}

/// <summary>
/// 初始化请求 DTO
/// </summary>
public record InitializeRequest(
    string Username,
    string Email,
    string Password);