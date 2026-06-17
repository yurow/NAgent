using System.Text.Json;
using Microsoft.Extensions.Logging;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services.Memory;
using NAgent.AgentDomain.Services.Skills;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentInfrastructure.Agents.LangChain;

/// <summary>
/// 基于 LangChain 的 Agent 引擎实现 - 集成 Skill 编排和 Tool 执行
/// 支持迭代式 Skill 调用，带防死循环机制
/// </summary>
public class LangChainAgentEngine : IAgentEngine
{
    private readonly ILlmClient _llmClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IMemorySystem _memorySystem;
    private readonly ILogger<LangChainAgentEngine>? _logger;

    /// <summary>
    /// 最大迭代次数 - 防止死循环的硬上限
    /// </summary>
    private const int MaxIterations = 5;

    public LangChainAgentEngine(
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        ISkillExecutor skillExecutor,
        IMemorySystem memorySystem,
        ILogger<LangChainAgentEngine>? logger = null)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _skillExecutor = skillExecutor ?? throw new ArgumentNullException(nameof(skillExecutor));
        _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentSession session,
        string userInput,
        string? modelId = null,
        CancellationToken cancellationToken = default,
        Action<DebugEvent>? onDebugEvent = null)
    {
        _debug = onDebugEvent; // 存储回调，供内部方法使用
        try
        {
            Emit("info", "执行开始", $"用户输入: {userInput}");

            // 1. 解析意图 - 模型选择 Skill
            var intent = await ParseSkillIntentAsync(session, userInput, cancellationToken);

            // 2. 如果不需要执行 Skill，直接生成回复
            if (!intent.NeedsExecution)
            {
                Emit("cot", "意图解析：直接回复", intent.Reasoning ?? "无 Skill 匹配，直接生成回复");
                var directResponse = await GenerateDirectResponseAsync(session, userInput, modelId, cancellationToken);
                Emit("llm", "生成回复", $"回复长度: {directResponse.Length} 字符");
                return new AgentExecutionResult(true, directResponse);
            }

            Emit("skill", $"选中 Skill: {intent.SkillName}", intent.Reasoning ?? "");

            // 3. 迭代执行 Skill（带防死循环机制）
            var iterationResult = await ExecuteSkillWithIterationAsync(
                session, userInput, intent, modelId, cancellationToken);

            return iterationResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Agent 执行失败");
            Emit("error", "执行失败", ex.Message);
            return new AgentExecutionResult(false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 发出调试事件
    /// </summary>
    private Action<DebugEvent>? _debug;
    private void Emit(string type, string title, string content = "", long durationMs = 0, string status = "info")
    {
        _debug?.Invoke(new DebugEvent
        {
            Type = type,
            Title = title,
            Content = content,
            DurationMs = durationMs,
            Status = status
        });
    }

    /// <summary>
    /// 迭代执行 Skill，带防死循环机制
    /// </summary>
    private async Task<AgentExecutionResult> ExecuteSkillWithIterationAsync(
        AgentSession session,
        string userInput,
        SkillSelectionResult initialIntent,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var currentIntent = initialIntent;
        var iteration = 0;
        var previousResults = new List<string>(); // 用于检测结果稳定性
        var allSteps = new List<SkillExecutionStep>();

        while (iteration < MaxIterations)
        {
            iteration++;
            _logger?.LogInformation(
                "Skill 迭代执行 [{Iteration}/{MaxIterations}]: {SkillName}",
                iteration, MaxIterations, currentIntent.SkillName);

            Emit("iteration", $"迭代 {iteration}/{MaxIterations}", $"执行 Skill: {currentIntent.SkillName}");

            // 执行当前 Skill
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var skillResult = await _skillExecutor.ExecuteAsync(
                currentIntent.SkillName,
                currentIntent.Parameters,
                session.ProjectId,
                cancellationToken);
            sw.Stop();

            // 记录每个工具步骤的执行详情
            foreach (var step in skillResult.Steps)
            {
                var paramStr = currentIntent.Parameters.Count > 0
                    ? string.Join(", ", currentIntent.Parameters.Select(p => $"{p.Key}={p.Value}"))
                    : "无参数";
                Emit(
                    step.Success ? "tool" : "tool",
                    $"工具: {step.ToolName}",
                    $"入参: {paramStr}\n结果: {(step.ToolOutput?.Length > 500 ? step.ToolOutput[..500] + "..." : step.ToolOutput)}",
                    sw.ElapsedMilliseconds,
                    step.Success ? "success" : "error"
                );
            }

            if (!skillResult.Success)
            {
                Emit("error", $"Skill '{currentIntent.SkillName}' 执行失败", skillResult.ErrorMessage ?? "");
                return new AgentExecutionResult(
                    false,
                    $"Skill '{currentIntent.SkillName}' 执行失败: {skillResult.ErrorMessage}",
                    currentIntent.SkillName);
            }

            allSteps.AddRange(skillResult.Steps);

            // 死循环检测 1: 检查是否达到最大迭代次数
            if (iteration >= MaxIterations)
            {
                _logger?.LogWarning("达到最大迭代次数 {MaxIterations}，强制退出", MaxIterations);
                Emit("warning", "达到最大迭代次数", $"已迭代 {MaxIterations} 次，强制返回结果");
                var finalResponse = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, "已达到最大处理次数，返回当前结果", modelId, cancellationToken);
                return new AgentExecutionResult(true, finalResponse, currentIntent.SkillName);
            }

            // 死循环检测 2: 检查 Skill 自身标记是否需要进一步处理
            if (!skillResult.NeedsFurtherProcessing)
            {
                _logger?.LogInformation("Skill 标记不需要进一步处理，退出迭代");
                Emit("decision", "Skill 完成，无需进一步处理", skillResult.Output?.Length > 200 ? skillResult.Output[..200] + "..." : skillResult.Output ?? "");
                var response = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, skillResult.Output, modelId, cancellationToken);
                return new AgentExecutionResult(true, response, currentIntent.SkillName);
            }

            // 死循环检测 3: 结果稳定性检测 - 如果结果与上次相似则退出
            if (previousResults.Count > 0 && IsResultSimilar(skillResult.Output, previousResults.Last()))
            {
                _logger?.LogWarning("检测到结果重复，退出迭代防止死循环");
                Emit("warning", "检测到死循环", "结果与上次迭代高度相似，自动退出");
                var stableResponse = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, skillResult.Output, modelId, cancellationToken);
                return new AgentExecutionResult(true, stableResponse, currentIntent.SkillName);
            }

            previousResults.Add(skillResult.Output);

            // 死循环检测 4: 空操作检测 - 如果结果为空或无变化则退出
            if (string.IsNullOrWhiteSpace(skillResult.Output) || skillResult.Steps.Count == 0)
            {
                _logger?.LogWarning("Skill 返回空结果或无执行步骤，退出迭代");
                var emptyResponse = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, "处理完成", modelId, cancellationToken);
                return new AgentExecutionResult(true, emptyResponse, currentIntent.SkillName);
            }

            // 5. 让模型判定是否需要继续调用 Skill
            var continueDecision = await EvaluateContinueAsync(
                session, userInput, skillResult, modelId, cancellationToken);

            if (!continueDecision.ShouldContinue)
            {
                _logger?.LogInformation("模型判定不需要继续处理，退出迭代");
                Emit("cot", "模型决策：不再继续", continueDecision.Reasoning);
                var finalResponse = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, skillResult.Output, modelId, cancellationToken);
                return new AgentExecutionResult(true, finalResponse, currentIntent.SkillName);
            }

            // 6. 准备下一次迭代
            if (!string.IsNullOrEmpty(continueDecision.NextSkillName))
            {
                Emit("cot", "模型决策：继续调用", $"下一步 Skill: {continueDecision.NextSkillName}\n理由: {continueDecision.Reasoning}");
                currentIntent = new SkillSelectionResult(
                    continueDecision.NextSkillName,
                    continueDecision.NextParameters,
                    continueDecision.Reasoning);
            }
            else if (!string.IsNullOrEmpty(skillResult.SuggestedNextSkill))
            {
                currentIntent = new SkillSelectionResult(
                    skillResult.SuggestedNextSkill,
                    currentIntent.Parameters,
                    "Skill 建议的下一步");
            }
            else
            {
                // 没有明确的下一步，退出迭代
                _logger?.LogInformation("没有明确的下一步 Skill，退出迭代");
                var noNextResponse = await GenerateResponseWithSkillResultAsync(
                    session, userInput, allSteps, skillResult.Output, modelId, cancellationToken);
                return new AgentExecutionResult(true, noNextResponse, currentIntent.SkillName);
            }
        }

        // 兜底：如果循环异常退出，返回最后结果
        var fallbackResponse = await GenerateResponseWithSkillResultAsync(
            session, userInput, allSteps, "处理完成", modelId, cancellationToken);
        return new AgentExecutionResult(true, fallbackResponse, currentIntent.SkillName);
    }

    /// <summary>
    /// 解析 Skill 意图 - 让模型选择 Skill 而非 Tool
    /// </summary>
    private async Task<SkillSelectionResult> ParseSkillIntentAsync(
        AgentSession session,
        string userInput,
        CancellationToken cancellationToken)
    {
        var prompt = BuildSkillIntentPrompt(session, userInput);

        try
        {
            var response = await _llmClient.GenerateAsync(prompt, cancellationToken: cancellationToken);

            var json = ExtractJson(response);
            if (!string.IsNullOrEmpty(json))
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var skillName = root.GetProperty("skill").GetString() ?? "";
                var reasoning = root.GetProperty("reasoning").GetString() ?? "";
                Emit("cot", "思维链：意图推理", reasoning);

                var parameters = new Dictionary<string, object>();
                if (root.TryGetProperty("parameters", out var paramsElement))
                {
                    foreach (var prop in paramsElement.EnumerateObject())
                    {
                        parameters[prop.Name] = prop.Value.ToString()!;
                    }
                }

                return new SkillSelectionResult(skillName, parameters, reasoning);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Skill 意图解析失败，将直接回复");
            Emit("warning", "意图解析失败", ex.Message);
        }

        return new SkillSelectionResult("", new Dictionary<string, object>(), "直接回复用户");
    }

    /// <summary>
    /// 评估是否需要继续处理
    /// </summary>
    private async Task<ContinueDecision> EvaluateContinueAsync(
        AgentSession session,
        string userInput,
        SkillExecutionResult lastResult,
        string? modelId,
        CancellationToken cancellationToken)
    {
        // 获取可用 Skills
        var skillsDesc = _skillExecutor.GetAvailableSkillsDescription();

        // 截断输出避免 prompt 太长
        var truncatedOutput = lastResult.Output?.Length > 2000
            ? lastResult.Output[..2000] + "\n...(结果已截断)"
            : lastResult.Output;

        // ⭐ 从搜索结果中提取可用的 URL，方便 LLM 直接引用
        var availableUrls = ExtractUrlsFromResult(lastResult.Output ?? "", 3);
        var urlsSection = availableUrls.Count > 0
            ? $"\n搜索结果中可抓取的 URL:\n{string.Join("\n", availableUrls.Select((u, i) => $"  {i + 1}. {u}"))}"
            : "\n搜索结果中未找到可抓取的 URL。";

        var prompt = $@"你是 NAgent AI 研究助手。你已经执行了搜索，现在需要决定下一步操作。

用户问题: {userInput}

已有搜索结果:
{truncatedOutput}
{urlsSection}

当前可用 Skills:
{skillsDesc}

【重要规则 - 按优先级执行】

1. **优先抓取 URL（最常用）**: 如果搜索结果中有相关链接但只有摘要，必须用 web_fetch 抓取 URL 获取详细内容。从上面列出的 URL 中选择最相关的 1 个。
   返回: {{""should_continue"": true, ""next_skill"": ""web_researcher"", ""next_parameters"": {{""url"": ""选中的URL""}}, ""reasoning"": ""搜索摘要不够详细，需要抓取该页面获取完整内容""}}

2. **换关键词搜索（仅在搜索结果完全不相关时使用）**: 如果搜索结果跟用户问题无关，换关键词再搜。
   返回: {{""should_continue"": true, ""next_skill"": ""web_researcher"", ""next_parameters"": {{""query"": ""新的搜索关键词""}}, ""reasoning"": ""搜索结果与问题不相关，需要换关键词""}}

3. **停止（结果已充分或无法继续）**: 如果搜索结果已充分回答问题，或多次搜索抓取后仍找不到更多信息，如实说明。
   返回: {{""should_continue"": false, ""reasoning"": ""已充分回答/已尽力搜索但找不到更多信息""}}

注意: next_parameters 中只传 url 或只传 query，不要同时传两个。不要编造信息。";

        try
        {
            var response = await _llmClient.GenerateAsync(prompt, modelId, cancellationToken: cancellationToken);
            var json = ExtractJson(response);

            if (!string.IsNullOrEmpty(json))
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var shouldContinue = root.GetProperty("should_continue").GetBoolean();
                var reasoning = root.GetProperty("reasoning").GetString() ?? "";
                Emit("cot", "思维链：继续决策", reasoning);

                if (shouldContinue)
                {
                    var nextSkill = root.GetProperty("next_skill").GetString() ?? "";
                    var nextParams = new Dictionary<string, object>();
                    if (root.TryGetProperty("next_parameters", out var paramsElement))
                    {
                        foreach (var prop in paramsElement.EnumerateObject())
                        {
                            nextParams[prop.Name] = prop.Value.ToString()!;
                        }
                    }
                    return new ContinueDecision(true, nextSkill, nextParams, reasoning);
                }

                return new ContinueDecision(false, null, null, reasoning);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "继续判定失败，默认不继续");
        }

        return new ContinueDecision(false, null, null, "判定失败，默认退出");
    }

    /// <summary>
    /// 从搜索结果输出中提取 URL
    /// </summary>
    private List<string> ExtractUrlsFromResult(string output, int maxUrls)
    {
        var urls = new List<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            output, @"链接:\s*(https?://\S+)", System.Text.RegularExpressions.RegexOptions.Multiline);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var url = match.Groups[1].Value.Trim();
            if (!urls.Contains(url) && urls.Count < maxUrls)
                urls.Add(url);
        }
        return urls;
    }

    /// <summary>
    /// 直接生成回复（不调用 Skill）
    /// </summary>
    private async Task<string> GenerateDirectResponseAsync(
        AgentSession session,
        string userInput,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var context = BuildContext(session, userInput);
        return await _llmClient.GenerateAsync(context, modelId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 基于 Skill 执行结果生成最终回复
    /// </summary>
    private async Task<string> GenerateResponseWithSkillResultAsync(
        AgentSession session,
        string userInput,
        List<SkillExecutionStep> allSteps,
        string finalOutput,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var stepsSummary = string.Join("\n", allSteps.Select(s =>
            $"步骤 {s.StepNumber}: {s.ToolName} - {(s.Success ? "成功" : "失败")}"));

        var prompt = $@"你是 NAgent AI 助手。用户提出了一个问题，你通过一系列 Skill/Tool 处理来获取信息。

用户问题: {userInput}

执行步骤:
{stepsSummary}

最终结果:
{finalOutput}

请基于以上结果，直接回答用户的问题。如果结果不足以回答问题，请说明。
回复:";

        return await _llmClient.GenerateAsync(prompt, modelId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 构建 Skill 意图解析 Prompt
    /// </summary>
    private string BuildSkillIntentPrompt(AgentSession session, string userInput)
    {
        var context = BuildContext(session, userInput);
        var availableSkills = _skillExecutor.GetAvailableSkillsDescription();
        var jsonExample1 = "{\"skill\": \"file_reader\", \"parameters\": {\"file_path\": \"init.md\"}, \"reasoning\": \"用户想读取init.md文件\"}";
        var jsonExample2 = "{\"skill\": \"\", \"parameters\": {}, \"reasoning\": \"用户只是闲聊，不需要调用Skill\"}";
        var example3 = "例如用户说'帮我写个hello.py'，你应该选择 file_writer Skill，参数 file_path='hello.py', content='print(\\\"hello\\\")'";
        var example4 = "例如用户说'记录一下小说名字为时光涟漪'，你应该选择 file_writer Skill，参数 file_path='notes.txt', content='小说名字: 时光涟漪'";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(context);
        sb.AppendLine();
        sb.AppendLine("【系统指令】");
        sb.AppendLine("你是 NAgent 的意图解析器。你的任务是根据用户的自然语言输入，自动选择合适的 Skill 并提取参数。");
        sb.AppendLine();
        sb.AppendLine("【重要规则】");
        sb.AppendLine("1. 用户用自然语言描述需求时，你必须自动推断参数，不要让用户手动输入命令");
        sb.AppendLine("2. 例如用户说'帮我读取init文件'，你应该选择 file_reader Skill，参数 file_path='init.md'");
        sb.AppendLine("3. 例如用户说'搜索一下GPT-4的最新消息'，你应该选择 web_researcher Skill，参数 query='GPT-4最新消息'");
        sb.AppendLine("4. " + example3);
        sb.AppendLine("5. " + example4);
        sb.AppendLine("6. 如果用户提到的文件名不完整（如'init'），根据工作目录文件列表自动补全（如'init.md'）");
        sb.AppendLine("7. 如果用户意图不明确或只是闲聊，返回空 skill");
        sb.AppendLine("8. 如果缺少必填参数（如文件路径），自动推断合理的默认值（如用 'notes.txt'、'record.txt' 等通用名称）");
        sb.AppendLine("9. 用户说'记录一下'、'记下来'、'保存'等，表示要写入文件，必须选择 file_writer Skill");
        sb.AppendLine();
        sb.AppendLine("可用 Skills:");
        sb.AppendLine(availableSkills);
        sb.AppendLine();
        sb.AppendLine("【输出格式】");
        sb.AppendLine("请严格返回以下 JSON 格式（不要包含其他文字）:");
        sb.AppendLine(jsonExample1);
        sb.AppendLine();
        sb.AppendLine("如果不需要调用 Skill，返回:");
        sb.AppendLine(jsonExample2);

        return sb.ToString();
    }

    /// <summary>
    /// 构建上下文
    /// </summary>
    private string BuildContext(AgentSession session, string currentInput)
    {
        var recentMessages = session.GetRecentMessages(5);
        var history = string.Join("\n", recentMessages.Select(m => $"{m.Role}: {m.Content}"));

        var workspacePath = session.GetContextVariable("workspace_path") ?? "未设置";
        var specContent = session.GetContextVariable("spec_content") ?? "";
        var workspaceFiles = session.GetContextVariable("workspace_files") ?? "";

        var sb = new System.Text.StringBuilder();

        // 会话内历史对话
        if (!string.IsNullOrWhiteSpace(history))
        {
            sb.AppendLine("历史对话:");
            sb.AppendLine(history);
            sb.AppendLine();
        }

        // 从记忆系统加载持久化的历史上下文
        try
        {
            var sessionId = Guid.TryParse(session.SessionKey, out var sid) ? sid : session.Id;
            var shortTermMemories = _memorySystem.GetShortTermMemoryAsync(session.ProjectId, sessionId, 10).GetAwaiter().GetResult();
            if (shortTermMemories.Count > 0)
            {
                sb.AppendLine("【近期对话记忆】");
                foreach (var mem in shortTermMemories)
                {
                    var roleLabel = mem.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "用户" : "AI";
                    sb.AppendLine($"{roleLabel}: {mem.Content}");
                }
                sb.AppendLine();
            }

            var longTermMemories = _memorySystem.GetLongTermMemoryAsync(session.ProjectId, sessionId, 5).GetAwaiter().GetResult();
            if (longTermMemories.Count > 0)
            {
                sb.AppendLine("【长期记忆摘要】");
                foreach (var mem in longTermMemories)
                {
                    var roleLabel = mem.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "用户" : "AI";
                    var contentPreview = mem.Content.Length > 200 ? mem.Content[..200] + "..." : mem.Content;
                    sb.AppendLine($"{roleLabel}: {contentPreview}");
                }
                sb.AppendLine();
            }

            var projectMemorySummaries = _memorySystem.GetProjectMemorySummaryAsync(session.ProjectId, 10).GetAwaiter().GetResult();
            if (projectMemorySummaries.Count > 0)
            {
                sb.AppendLine("【项目知识记忆】");
                foreach (var pms in projectMemorySummaries)
                {
                    sb.AppendLine($"- [{pms.Category}] {pms.Summary}");
                }
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "加载记忆上下文失败，跳过");
        }

        sb.AppendLine($"当前工作目录: {workspacePath}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(workspaceFiles))
        {
            sb.AppendLine("工作目录文件列表:");
            sb.AppendLine(workspaceFiles);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(specContent))
        {
            var specPreview = specContent.Length > 3000
                ? specContent[..3000] + "\n\n... [spec.md 内容已截断]"
                : specContent;
            sb.AppendLine("项目规范文档 (spec.md):");
            sb.AppendLine(specPreview);
            sb.AppendLine();
        }

        sb.AppendLine($"当前输入: {currentInput}");

        return sb.ToString();
    }

    /// <summary>
    /// 从文本中提取 JSON
    /// </summary>
    private string? ExtractJson(string text)
    {
        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return text[startIndex..(endIndex + 1)];
        }

        return null;
    }

    /// <summary>
    /// 判断两个结果是否相似（用于死循环检测）
    /// </summary>
    private bool IsResultSimilar(string result1, string result2)
    {
        if (string.IsNullOrEmpty(result1) || string.IsNullOrEmpty(result2))
            return false;

        // 简单相似度：计算共同子串长度比例
        var minLen = Math.Min(result1.Length, result2.Length);
        if (minLen == 0) return false;

        var commonPrefixLen = 0;
        for (int i = 0; i < minLen; i++)
        {
            if (result1[i] == result2[i])
                commonPrefixLen++;
            else
                break;
        }

        // 如果共同前缀超过 80%，认为结果相似
        return (double)commonPrefixLen / minLen > 0.8;
    }

    public async Task<ToolSelectionResult> ParseIntentAsync(
        AgentSession session,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        // 兼容旧接口：将 Skill 选择转换为 Tool 选择
        var skillIntent = await ParseSkillIntentAsync(session, userInput, cancellationToken);
        if (!skillIntent.NeedsExecution)
        {
            return new ToolSelectionResult("", new Dictionary<string, object>(), skillIntent.Reasoning);
        }

        // 如果选择了 Skill，获取该 Skill 的第一个工具作为代表
        var skill = await _skillExecutor.ExecuteAsync(
            skillIntent.SkillName, skillIntent.Parameters, session.ProjectId, cancellationToken);

        if (skill.Steps.Count > 0)
        {
            return new ToolSelectionResult(
                skill.Steps[0].ToolName,
                skillIntent.Parameters,
                $"通过 Skill '{skillIntent.SkillName}' 调用");
        }

        return new ToolSelectionResult("", new Dictionary<string, object>(), "无可用工具");
    }

    public async Task<string> GenerateResponseAsync(
        AgentSession session,
        string context,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"基于以下上下文，生成友好、专业的回复：

{context}

回复：";
        return await _llmClient.GenerateAsync(prompt, modelId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 继续处理判定结果
    /// </summary>
    private class ContinueDecision
    {
        public bool ShouldContinue { get; }
        public string? NextSkillName { get; }
        public Dictionary<string, object>? NextParameters { get; }
        public string Reasoning { get; }

        public ContinueDecision(bool shouldContinue, string? nextSkillName, Dictionary<string, object>? nextParameters, string reasoning)
        {
            ShouldContinue = shouldContinue;
            NextSkillName = nextSkillName;
            NextParameters = nextParameters;
            Reasoning = reasoning;
        }
    }
}