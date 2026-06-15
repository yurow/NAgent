using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentDomain.Services.Tools;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 工具管理 API - 查询系统内置工具
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(IToolRegistry toolRegistry, ILogger<ToolsController> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有可用工具列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ToolInfoDto>>), StatusCodes.Status200OK)]
    public IActionResult GetAllTools()
    {
        var tools = _toolRegistry.GetAllTools();
        var dtos = tools.Select(t => new ToolInfoDto(
            t.ToolName,
            t.Description,
            "built-in",
            "Low",
            "system",
            true
        )).ToList();

        return Ok(ApiResponse<List<ToolInfoDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// 获取指定工具详情
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ApiResponse<ToolInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetTool(string name)
    {
        var tool = _toolRegistry.GetTool(name);
        if (tool == null)
            return NotFound(ApiResponse.FailureResponse($"工具 '{name}' 不存在"));

        var dto = new ToolInfoDto(
            tool.ToolName,
            tool.Description,
            "built-in",
            "Low",
            "system",
            true
        );

        return Ok(ApiResponse<ToolInfoDto>.SuccessResponse(dto));
    }
}

/// <summary>
/// 工具信息 DTO
/// </summary>
public record ToolInfoDto(
    string Name,
    string Description,
    string Category,
    string SecurityLevel,
    string Source,
    bool IsEnabled
);
