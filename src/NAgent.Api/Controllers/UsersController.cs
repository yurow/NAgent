using MediatR;
using Microsoft.AspNetCore.Mvc;
using NAgent.Application.Features.Users.Commands;
using NAgent.Application.Features.Users.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
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
}

public record CreateUserRequest(
    string Username,
    string Email
);
