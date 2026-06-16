using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services;
using NAgent.AgentDomain.Services.Tools;
using NAgent.Shared.Responses;

namespace NAgent.Api.Controllers;

/// <summary>
/// 工具管理 API - 查询内置工具和 YAML 配置工具
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IToolDefinitionRepository _toolDefinitionRepository;
    private readonly IToolLoader _toolLoader;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(
        IToolRegistry toolRegistry,
        IToolDefinitionRepository toolDefinitionRepository,
        IToolLoader toolLoader,
        IConfiguration configuration,
        ILogger<ToolsController> logger)
    {
        _toolRegistry = toolRegistry;
        _toolDefinitionRepository = toolDefinitionRepository;
        _toolLoader = toolLoader;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有可用工具列表（内置工具 + YAML 配置工具）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ToolInfoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTools(CancellationToken cancellationToken)
    {
        var dtos = new List<ToolInfoDto>();

        // 1. 添加内置工具（从 ToolRegistry）
        var builtInTools = _toolRegistry.GetAllTools();
        var builtInNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in builtInTools)
        {
            builtInNames.Add(tool.ToolName);
            dtos.Add(new ToolInfoDto(
                tool.ToolName,
                tool.Description,
                "built-in",
                "Low",
                "system",
                true,
                null
            ));
        }

        // 2. 添加 YAML 配置工具（从 ToolDefinitionRepository，跳过与内置同名的）
        var yamlTools = await _toolDefinitionRepository.GetAllAsync(cancellationToken);
        foreach (var tool in yamlTools)
        {
            if (builtInNames.Contains(tool.Name))
                continue;

            dtos.Add(new ToolInfoDto(
                tool.Name,
                tool.Description,
                tool.Category,
                tool.SecurityLevel.ToString(),
                $"tools/{tool.FilePath}",
                tool.IsEnabled,
                tool.YamlContent
            ));
        }

        return Ok(ApiResponse<List<ToolInfoDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// 获取指定工具详情
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ApiResponse<ToolInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTool(string name, CancellationToken cancellationToken)
    {
        // 先查内置工具
        var builtIn = _toolRegistry.GetTool(name);
        if (builtIn != null)
        {
            var dto = new ToolInfoDto(
                builtIn.ToolName,
                builtIn.Description,
                "built-in",
                "Low",
                "system",
                true,
                null
            );
            return Ok(ApiResponse<ToolInfoDto>.SuccessResponse(dto));
        }

        // 再查 YAML 工具
        var yamlTool = await _toolDefinitionRepository.GetByNameAsync(name, cancellationToken);
        if (yamlTool == null)
            return NotFound(ApiResponse.FailureResponse($"工具 '{name}' 不存在"));

        var yamlDto = new ToolInfoDto(
            yamlTool.Name,
            yamlTool.Description,
            yamlTool.Category,
            yamlTool.SecurityLevel.ToString(),
            $"tools/{yamlTool.FilePath}",
            yamlTool.IsEnabled,
            yamlTool.YamlContent
        );

        return Ok(ApiResponse<ToolInfoDto>.SuccessResponse(yamlDto));
    }

    /// <summary>
    /// 重新加载所有 YAML 配置工具
    /// </summary>
    [HttpPost("reload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReloadAll(CancellationToken cancellationToken)
    {
        try
        {
            var toolsDir = _configuration["Tools:Directory"];
            if (string.IsNullOrEmpty(toolsDir))
                toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");

            if (!Directory.Exists(toolsDir))
                return Ok(ApiResponse<int>.SuccessResponse(0, "Tools 目录不存在"));

            var tools = await _toolLoader.LoadFromDirectoryAsync(toolsDir, cancellationToken);
            int loadedCount = 0;

            foreach (var tool in tools)
            {
                var existing = await _toolDefinitionRepository.GetByNameAsync(tool.Name, cancellationToken);
                if (existing == null)
                {
                    await _toolDefinitionRepository.AddAsync(tool, cancellationToken);
                    loadedCount++;
                }
            }

            return Ok(ApiResponse<int>.SuccessResponse(loadedCount, $"成功加载 {loadedCount} 个新工具，共扫描 {tools.Count} 个工具定义"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载 Tools 失败");
            return StatusCode(500, ApiResponse.FailureResponse($"重新加载失败: {ex.Message}"));
        }
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
    bool IsEnabled,
    string? Content = null
);
