using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentDomain.Services.Skills;

/// <summary>
/// Skill 执行结果
/// </summary>
public class SkillExecutionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 执行输出（聚合多个 Tool 的结果）
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行的步骤记录
    /// </summary>
    public List<SkillExecutionStep> Steps { get; set; } = new();

    /// <summary>
    /// 是否需要进一步处理（用于迭代判定）
    /// </summary>
    public bool NeedsFurtherProcessing { get; set; }

    /// <summary>
    /// 建议的下一步 Skill（如果需要进一步处理）
    /// </summary>
    public string? SuggestedNextSkill { get; set; }

    public static SkillExecutionResult Ok(string output, List<SkillExecutionStep>? steps = null)
    {
        return new SkillExecutionResult
        {
            Success = true,
            Output = output,
            Steps = steps ?? new List<SkillExecutionStep>(),
            NeedsFurtherProcessing = false
        };
    }

    public static SkillExecutionResult Fail(string errorMessage)
    {
        return new SkillExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            NeedsFurtherProcessing = false
        };
    }
}

/// <summary>
/// Skill 执行步骤记录
/// </summary>
public class SkillExecutionStep
{
    public int StepNumber { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ToolOutput { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Skill 选择结果（替代 ToolSelectionResult）
/// </summary>
public class SkillSelectionResult
{
    /// <summary>
    /// 选中的 Skill 名称
    /// </summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Skill 参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// 推理过程
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要执行 Skill
    /// </summary>
    public bool NeedsExecution => !string.IsNullOrEmpty(SkillName);

    public SkillSelectionResult(string skillName, Dictionary<string, object> parameters, string reasoning)
    {
        SkillName = skillName;
        Parameters = parameters;
        Reasoning = reasoning;
    }
}

/// <summary>
/// Skill 执行器接口 - 负责解析 Skill 定义并编排 Tool 调用
/// </summary>
public interface ISkillExecutor
{
    /// <summary>
    /// 执行 Skill
    /// </summary>
    /// <param name="skillName">Skill 名称</param>
    /// <param name="parameters">用户参数</param>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<SkillExecutionResult> ExecuteAsync(
        string skillName,
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有可用 Skills 描述（用于 Prompt）
    /// </summary>
    string GetAvailableSkillsDescription();
}
