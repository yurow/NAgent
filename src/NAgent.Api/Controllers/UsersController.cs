using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.Application.Features.Users.Commands;
using NAgent.Application.Features.Users.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 获取所有用户（管理员）
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<Application.DTOs.UserDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllUsersQuery();
        var users = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<Application.DTOs.UserDto>>.SuccessResponse(users));
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Application.DTOs.UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Application.DTOs.UserDto>>> GetById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.Send(query);

        return Ok(ApiResponse<Application.DTOs.UserDto>.SuccessResponse(user));
    }

    /// <summary>
    /// 创建用户（管理员）
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateUser([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Username, request.Email);
        var userId = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById), 
            new { id = userId }, 
            ApiResponse<Guid>.SuccessResponse(userId, "用户创建成功"));
    }

    /// <summary>
    /// 更新用户状态（启用/禁用）
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserStatusCommand(id, request.IsActive);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "用户状态更新成功"));
    }

    /// <summary>
    /// 更新用户角色（管理员）
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserRoleCommand(id, request.IsAdmin);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "用户角色更新成功"));
    }

    /// <summary>
    /// 重置用户密码（管理员）
    /// </summary>
    [HttpPut("{id:guid}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetUserPasswordCommand(id, request.NewPassword);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "密码重置成功"));
    }
}

public record CreateUserRequest(
    string Username,
    string Email
);

public record UpdateUserStatusRequest(bool IsActive);

public record UpdateUserRoleRequest(bool IsAdmin);

public record ResetPasswordRequest(string NewPassword);
