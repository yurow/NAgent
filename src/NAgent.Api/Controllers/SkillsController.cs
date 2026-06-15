using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentApplication.Features.Skills.Commands;
using NAgent.AgentApplication.Features.Skills.Queries;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// Skills 管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// 获取所有 Skills
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllSkillsQuery();
        var skills = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<SkillDto>>.SuccessResponse(skills));
    }

    /// <summary>
    /// 获取启用的 Skills
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetEnabled(CancellationToken cancellationToken)
    {
        var query = new GetEnabledSkillsQuery();
        var skills = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<SkillDto>>.SuccessResponse(skills));
    }

    /// <summary>
    /// 根据分类获取 Skills
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetByCategory(string category, CancellationToken cancellationToken)
    {
        var query = new GetSkillsByCategoryQuery(category);
        var skills = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<List<SkillDto>>.SuccessResponse(skills));
    }

    /// <summary>
    /// 根据名称获取 Skill
    /// </summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<ApiResponse<SkillDto?>>> GetByName(string name, CancellationToken cancellationToken)
    {
        var query = new GetSkillByNameQuery(name);
        var skill = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<SkillDto?>.SuccessResponse(skill));
    }

    /// <summary>
    /// 从目录加载 Skills
    /// </summary>
    [HttpPost("load")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> LoadFromDirectory([FromBody] LoadSkillsRequest request, CancellationToken cancellationToken)
    {
        var command = new LoadSkillsFromDirectoryCommand(request.DirectoryPath);
        var count = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(count, $"成功加载 {count} 个 Skills"));
    }

    /// <summary>
    /// 重新加载所有 Skills
    /// </summary>
    [HttpPost("reload")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> ReloadAll(CancellationToken cancellationToken)
    {
        var command = new ReloadAllSkillsCommand();
        var count = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(count, $"成功重新加载 {count} 个 Skills"));
    }

    /// <summary>
    /// 启用/禁用 Skill
    /// </summary>
    [HttpPost("{id}/enabled")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SetEnabled(Guid id, [FromBody] SetSkillEnabledRequest request, CancellationToken cancellationToken)
    {
        var command = new SetSkillEnabledCommand(id, request.IsEnabled);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(result));
    }
}

/// <summary>
/// 加载 Skills 请求
/// </summary>
public record LoadSkillsRequest(string DirectoryPath);

/// <summary>
/// 设置 Skill 启用状态请求
/// </summary>
public record SetSkillEnabledRequest(bool IsEnabled);
