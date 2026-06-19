using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.ManageRoles.Commands;
using NAgent.AgentApplication.Features.ManageRoles.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 角色管理控制器
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId}/roles")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 获取项目的所有角色
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<List<AgentRoleDto>>> GetRoles(Guid projectId)
    {
        var roles = await _mediator.Send(new GetProjectRolesQuery { ProjectId = projectId });
        return ApiResponse<List<AgentRoleDto>>.SuccessResponse(roles);
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{roleId}")]
    public async Task<ApiResponse<AgentRoleDto?>> GetRole(Guid projectId, Guid roleId)
    {
        var role = await _mediator.Send(new GetRoleByIdQuery { RoleId = roleId });
        return ApiResponse<AgentRoleDto?>.SuccessResponse(role);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<Guid>> CreateRole(Guid projectId, [FromBody] CreateRoleRequest request)
    {
        var command = new CreateRoleCommand(
            projectId, request.Name, request.Description, request.SystemPrompt, request.ModelId);
        var roleId = await _mediator.Send(command);
        return ApiResponse<Guid>.SuccessResponse(roleId, "角色创建成功");
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{roleId}")]
    public async Task<ApiResponse<bool>> UpdateRole(Guid projectId, Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        var command = new UpdateRoleCommand(
            roleId, request.Name, request.Description, request.SystemPrompt, request.ModelId);
        var result = await _mediator.Send(command);
        return ApiResponse<bool>.SuccessResponse(result, "角色更新成功");
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{roleId}")]
    public async Task<ApiResponse<bool>> DeleteRole(Guid projectId, Guid roleId)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(roleId));
        return ApiResponse<bool>.SuccessResponse(result, "角色删除成功");
    }

    /// <summary>
    /// 激活角色（设为项目当前角色）
    /// </summary>
    [HttpPost("{roleId}/activate")]
    public async Task<ApiResponse<bool>> ActivateRole(Guid projectId, Guid roleId)
    {
        var result = await _mediator.Send(new SetActiveRoleCommand(projectId, roleId));
        return ApiResponse<bool>.SuccessResponse(result, "角色已激活");
    }

    /// <summary>
    /// 取消激活角色
    /// </summary>
    [HttpPost("deactivate")]
    public async Task<ApiResponse<bool>> DeactivateRole(Guid projectId)
    {
        var result = await _mediator.Send(new SetActiveRoleCommand(projectId, null));
        return ApiResponse<bool>.SuccessResponse(result, "已取消激活角色");
    }
}

/// <summary>
/// 创建角色请求
/// </summary>
public record CreateRoleRequest(string Name, string Description, string SystemPrompt, Guid? ModelId);

/// <summary>
/// 更新角色请求
/// </summary>
public record UpdateRoleRequest(string Name, string Description, string SystemPrompt, Guid? ModelId);
