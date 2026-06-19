using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.Projects.Commands;
using NAgent.AgentApplication.Features.Projects.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 项目管理控制器
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 获取用户的所有项目
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ApiResponse<List<ProjectDto>>> GetUserProjects(Guid userId)
    {
        var projects = await _mediator.Send(new GetUserProjectsQuery { UserId = userId });
        return ApiResponse<List<ProjectDto>>.SuccessResponse(projects);
    }

    /// <summary>
    /// 获取用户的活跃项目
    /// </summary>
    [HttpGet("active/{userId}")]
    public async Task<ApiResponse<ProjectDto?>> GetActiveProject(Guid userId)
    {
        var project = await _mediator.Send(new GetActiveProjectQuery { UserId = userId });
        return ApiResponse<ProjectDto?>.SuccessResponse(project);
    }

    /// <summary>
    /// 获取项目详情
    /// </summary>
    [HttpGet("{projectId}")]
    public async Task<ApiResponse<ProjectDto?>> GetProjectById(Guid projectId)
    {
        var project = await _mediator.Send(new GetProjectByIdQuery { ProjectId = projectId });
        return ApiResponse<ProjectDto?>.SuccessResponse(project);
    }

    /// <summary>
    /// 检查项目是否存在
    /// </summary>
    [HttpGet("exists")]
    public async Task<ApiResponse<bool>> ProjectExists(Guid userId, string projectName)
    {
        var exists = await _mediator.Send(new ProjectExistsQuery { UserId = userId, ProjectName = projectName });
        return ApiResponse<bool>.SuccessResponse(exists);
    }

    /// <summary>
    /// 创建新项目
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<Guid>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var command = new CreateProjectCommand(request.Name, request.Description, request.UserId);
        var projectId = await _mediator.Send(command);
        return ApiResponse<Guid>.SuccessResponse(projectId, "项目创建成功");
    }

    /// <summary>
    /// 激活项目
    /// </summary>
    [HttpPost("{projectId}/activate")]
    public async Task<ApiResponse<bool>> ActivateProject(Guid projectId, [FromBody] ActivateProjectRequest request)
    {
        var command = new ActivateProjectCommand(projectId, request.UserId);
        var result = await _mediator.Send(command);
        return ApiResponse<bool>.SuccessResponse(result, "项目激活成功");
    }

    /// <summary>
    /// 停用项目
    /// </summary>
    [HttpPost("{projectId}/deactivate")]
    public async Task<ApiResponse<bool>> DeactivateProject(Guid projectId)
    {
        var result = await _mediator.Send(new DeactivateProjectCommand(projectId));
        return ApiResponse<bool>.SuccessResponse(result, "项目停用成功");
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    [HttpPut("{projectId}")]
    public async Task<ApiResponse<bool>> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest request)
    {
        var command = new UpdateProjectCommand(projectId, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return ApiResponse<bool>.SuccessResponse(result, "项目更新成功");
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    [HttpDelete("{projectId}")]
    public async Task<ApiResponse<bool>> DeleteProject(Guid projectId, [FromBody] DeleteProjectRequest request)
    {
        var command = new DeleteProjectCommand(projectId, request.UserId);
        var result = await _mediator.Send(command);
        return ApiResponse<bool>.SuccessResponse(result, "项目删除成功");
    }
}

/// <summary>
/// 创建项目请求
/// </summary>
public record CreateProjectRequest(string Name, string Description, Guid UserId);

/// <summary>
/// 激活项目请求
/// </summary>
public record ActivateProjectRequest(Guid UserId);

/// <summary>
/// 更新项目请求
/// </summary>
public record UpdateProjectRequest(string Name, string Description);

/// <summary>
/// 删除项目请求
/// </summary>
public record DeleteProjectRequest(Guid UserId);
