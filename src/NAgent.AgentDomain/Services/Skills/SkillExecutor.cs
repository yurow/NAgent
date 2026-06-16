using System.Text.Json;
using System.Text.RegularExpressions;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentDomain.Services.Skills;

/// <summary>
/// Skill 执行器实现 - 解析 Skill 定义并编排 Tool 调用
/// </summary>
public class SkillExecutor : ISkillExecutor
{
    private readonly ISkillRepository _skillRepository;
    private readonly IToolRegistry _toolRegistry;

    public SkillExecutor(ISkillRepository skillRepository, IToolRegistry toolRegistry)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    public string GetAvailableSkillsDescription()
    {
        var skills = _skillRepository.GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        var lines = new List<string>();
        foreach (var skill in skills.Where(s => s.IsEnabled))
        {
            lines.Add($"- {skill.Name}: {skill.Description}");
            if (skill.ToolNames.Count > 0)
            {
                lines.Add($"  包含工具: {string.Join(", ", skill.ToolNames)}");
            }
            // 提取参数说明
            var paramDescriptions = ExtractParameterDescriptions(skill);
            if (paramDescriptions.Count > 0)
            {
                lines.Add($"  参数:");
                foreach (var pd in paramDescriptions)
                {
                    lines.Add($"    - {pd.Key}: {pd.Value}");
                }
            }
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 从 Skill Markdown 内容中提取参数说明
    /// </summary>
    private Dictionary<string, string> ExtractParameterDescriptions(Skill skill)
    {
        var result = new Dictionary<string, string>();
        var content = skill.MarkdownContent;

        // 匹配 ## 参数 部分下的列表项
        var paramSectionMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @"##\s*参数\s*\n(.*?)(?=\n##|\z)",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (paramSectionMatch.Success)
        {
            var paramSection = paramSectionMatch.Groups[1].Value;
            var paramMatches = System.Text.RegularExpressions.Regex.Matches(
                paramSection,
                @"-\s*`([^`]+)`:\s*(.+?)(?=\n-\s*`|\z)");

            foreach (System.Text.RegularExpressions.Match match in paramMatches)
            {
                var paramName = match.Groups[1].Value.Trim();
                var paramDesc = match.Groups[2].Value.Trim().Replace("\n", " ");
                result[paramName] = paramDesc;
            }
        }

        return result;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(
        string skillName,
        Dictionary<string, object> parameters,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // 1. 查找 Skill
        var skill = await _skillRepository.GetByNameAsync(skillName, cancellationToken);
        if (skill == null)
        {
            return SkillExecutionResult.Fail($"Skill '{skillName}' 不存在");
        }

        if (!skill.IsEnabled)
        {
            return SkillExecutionResult.Fail($"Skill '{skillName}' 已禁用");
        }

        // 2. 解析 Skill 的 MarkdownContent，提取编排逻辑
        var orchestration = ParseOrchestration(skill);

        // 3. 按编排顺序执行工具
        var steps = new List<SkillExecutionStep>();
        var aggregatedOutput = new System.Text.StringBuilder();
        aggregatedOutput.AppendLine($"=== Skill: {skill.Name} ===");
        aggregatedOutput.AppendLine($"描述: {skill.Description}");
        aggregatedOutput.AppendLine();

        int stepNumber = 1;
        foreach (var step in orchestration.Steps)
        {
            // 检查条件
            if (!string.IsNullOrEmpty(step.Condition))
            {
                if (!EvaluateCondition(step.Condition, parameters))
                {
                    aggregatedOutput.AppendLine($"[步骤 {stepNumber}] {step.ToolName}: 条件不满足，跳过");
                    continue;
                }
            }

            // 获取工具
            var tool = _toolRegistry.GetTool(step.ToolName);
            if (tool == null)
            {
                var errorMsg = $"工具 '{step.ToolName}' 未找到";
                steps.Add(new SkillExecutionStep
                {
                    StepNumber = stepNumber,
                    ToolName = step.ToolName,
                    ToolOutput = errorMsg,
                    Success = false,
                    ExecutedAt = DateTime.UtcNow
                });
                return SkillExecutionResult.Fail(errorMsg);
            }

            // 构建工具参数（合并用户参数和步骤参数映射）
            var toolParams = BuildToolParameters(step, parameters);

            // 执行工具
            var toolResult = await tool.ExecuteAsync(toolParams, projectId, cancellationToken);

            steps.Add(new SkillExecutionStep
            {
                StepNumber = stepNumber,
                ToolName = step.ToolName,
                ToolOutput = toolResult.Success ? toolResult.Output : (toolResult.ErrorMessage ?? "失败"),
                Success = toolResult.Success,
                ExecutedAt = DateTime.UtcNow
            });

            if (!toolResult.Success)
            {
                aggregatedOutput.AppendLine($"[步骤 {stepNumber}] {step.ToolName}: 失败 - {toolResult.ErrorMessage}");
                return SkillExecutionResult.Fail($"步骤 {stepNumber} 失败: {toolResult.ErrorMessage}");
            }

            aggregatedOutput.AppendLine($"[步骤 {stepNumber}] {step.ToolName}: 成功");
            aggregatedOutput.AppendLine(toolResult.Output);
            aggregatedOutput.AppendLine();

            // 将工具输出加入参数，供后续步骤使用
            parameters[$"{step.ToolName}_result"] = toolResult.Output;

            stepNumber++;
        }

        // 4. 如果 Skill 没有定义编排步骤，则按 ToolNames 顺序执行
        if (orchestration.Steps.Count == 0 && skill.ToolNames.Count > 0)
        {
            foreach (var toolName in skill.ToolNames)
            {
                var tool = _toolRegistry.GetTool(toolName);
                if (tool == null) continue;

                var toolResult = await tool.ExecuteAsync(parameters, projectId, cancellationToken);

                steps.Add(new SkillExecutionStep
                {
                    StepNumber = stepNumber,
                    ToolName = toolName,
                    ToolOutput = toolResult.Success ? toolResult.Output : (toolResult.ErrorMessage ?? "失败"),
                    Success = toolResult.Success,
                    ExecutedAt = DateTime.UtcNow
                });

                if (!toolResult.Success)
                {
                    return SkillExecutionResult.Fail($"工具 {toolName} 失败: {toolResult.ErrorMessage}");
                }

                aggregatedOutput.AppendLine($"[步骤 {stepNumber}] {toolName}: 成功");
                aggregatedOutput.AppendLine(toolResult.Output);
                aggregatedOutput.AppendLine();

                parameters[$"{toolName}_result"] = toolResult.Output;
                stepNumber++;
            }
        }

        var result = SkillExecutionResult.Ok(aggregatedOutput.ToString(), steps);

        // 5. 判定是否需要进一步处理（基于 Skill 内容中的标记）
        result.NeedsFurtherProcessing = orchestration.NeedsFurtherProcessing;
        result.SuggestedNextSkill = orchestration.SuggestedNextSkill;

        return result;
    }

    /// <summary>
    /// 解析 Skill Markdown 中的编排逻辑
    /// </summary>
    private OrchestrationPlan ParseOrchestration(Skill skill)
    {
        var plan = new OrchestrationPlan();
        var content = skill.MarkdownContent;

        // 尝试提取 YAML frontmatter
        var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
        if (yamlMatch.Success)
        {
            var yaml = yamlMatch.Groups[1].Value;

            // 解析 tools 列表
            var toolsMatch = Regex.Match(yaml, @"tools:\s*\[(.*?)\]");
            if (toolsMatch.Success)
            {
                var tools = toolsMatch.Groups[1].Value
                    .Split(',')
                    .Select(t => t.Trim().Trim('\'', '\"'))
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                // 如果 Markdown 中定义了 tools，但 Skill.ToolNames 为空，则补充
                foreach (var tool in tools)
                {
                    if (!skill.ToolNames.Contains(tool))
                    {
                        skill.GetType().GetProperty("ToolNames")?.SetValue(skill,
                            new List<string>(skill.ToolNames) { tool });
                    }
                }
            }

            // 解析 needs_further_processing 标记
            if (yaml.Contains("needs_further_processing: true"))
            {
                plan.NeedsFurtherProcessing = true;
            }

            // 解析 suggested_next_skill
            var nextSkillMatch = Regex.Match(yaml, "suggested_next_skill:\\s*['\"]?(.*?)['\"]?\\s*$", RegexOptions.Multiline);
            if (nextSkillMatch.Success)
            {
                plan.SuggestedNextSkill = nextSkillMatch.Groups[1].Value.Trim();
            }
        }

        // 解析步骤标记 <!-- step: tool_name -->
        var stepMatches = Regex.Matches(content, @"<!--\s*step:\s*(\w+)\s*(?:\|\s*condition:\s*(.*?))?\s*-->");
        foreach (Match match in stepMatches)
        {
            var toolName = match.Groups[1].Value.Trim();
            var condition = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            plan.Steps.Add(new OrchestrationStep
            {
                ToolName = toolName,
                Condition = condition
            });
        }

        // 如果没有显式步骤，但有 ToolNames，则按顺序生成步骤
        if (plan.Steps.Count == 0 && skill.ToolNames.Count > 0)
        {
            foreach (var toolName in skill.ToolNames)
            {
                plan.Steps.Add(new OrchestrationStep { ToolName = toolName });
            }
        }

        return plan;
    }

    /// <summary>
    /// 评估条件表达式
    /// </summary>
    private bool EvaluateCondition(string condition, Dictionary<string, object> parameters)
    {
        // 简单条件评估：检查参数是否存在且非空
        var paramName = condition.Trim();
        if (parameters.TryGetValue(paramName, out var value))
        {
            return value != null && !string.IsNullOrEmpty(value.ToString());
        }
        return false;
    }

    /// <summary>
    /// 构建工具参数
    /// </summary>
    private Dictionary<string, object> BuildToolParameters(OrchestrationStep step, Dictionary<string, object> userParams)
    {
        var result = new Dictionary<string, object>(userParams, StringComparer.OrdinalIgnoreCase);

        // 如果有参数映射，应用映射
        if (step.ParameterMapping != null)
        {
            foreach (var mapping in step.ParameterMapping)
            {
                if (userParams.TryGetValue(mapping.Value, out var value))
                {
                    result[mapping.Key] = value;
                }
            }
        }

        return result;
    }

    private class OrchestrationPlan
    {
        public List<OrchestrationStep> Steps { get; set; } = new();
        public bool NeedsFurtherProcessing { get; set; }
        public string? SuggestedNextSkill { get; set; }
    }

    private class OrchestrationStep
    {
        public string ToolName { get; set; } = string.Empty;
        public string? Condition { get; set; }
        public Dictionary<string, string>? ParameterMapping { get; set; }
    }
}
