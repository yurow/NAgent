using System.Text.Json;
using Microsoft.Extensions.Logging;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Memory;

namespace NAgent.AgentInfrastructure.Services;

/// <summary>
/// 意图分类与推测服务实现
/// </summary>
public class IntentServiceImpl : IIntentService
{
    private readonly ILlmClient _llmClient;
    private readonly IMemorySystem _memorySystem;
    private readonly IToolDefinitionRepository _toolDefRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly ILogger<IntentServiceImpl> _logger;

    /// <summary>
    /// 固定意图枚举定义（标识 -> 中文描述）
    /// </summary>
    private static readonly Dictionary<string, string> IntentEnums = new()
    {
        ["code_generation"] = "代码生成：编写新代码、函数、类或模块",
        ["code_analysis"] = "代码分析：理解、解释或审查现有代码",
        ["code_debug"] = "代码调试：定位和修复 bug 或错误",
        ["file_read"] = "文件读取：查看或搜索文件内容",
        ["file_write"] = "文件写入：创建、修改或更新文件",
        ["web_search"] = "网络搜索：查询互联网上的实时信息",
        ["project_structure"] = "项目结构：浏览文件目录或了解项目布局",
        ["documentation"] = "文档生成：编写注释、README 或技术文档",
        ["data_processing"] = "数据处理：解析、转换或分析数据",
        ["architecture_design"] = "架构设计：讨论系统设计、技术选型",
        ["content_creation"] = "内容创作：写小说、文章、故事、剧本等创意内容",
        ["summarization"] = "内容总结：概括、提炼、摘要文档或文本内容",
        ["translation"] = "翻译：将文本从一种语言翻译为另一种语言",
        ["general_chat"] = "一般对话：闲聊、问候或非技术性问题"
    };

    public IntentServiceImpl(
        ILlmClient llmClient,
        IMemorySystem memorySystem,
        IToolDefinitionRepository toolDefRepo,
        ISkillRepository skillRepo,
        ILogger<IntentServiceImpl> logger)
    {
        _llmClient = llmClient;
        _memorySystem = memorySystem;
        _toolDefRepo = toolDefRepo;
        _skillRepo = skillRepo;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IntentClassificationResult> ClassifyIntentAsync(
        string userInput,
        List<ChatSummary> recentSummaries,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 构建固定意图枚举描述
            var intentListStr = string.Join("\n",
                IntentEnums.Select(kv => $"- {kv.Key}: {kv.Value}"));

            // 构建简短对话摘要（最多 2 轮）
            var summaryStr = recentSummaries.Count > 0
                ? "\n\n最近对话摘要（仅用于消除歧义）:\n" + string.Join("\n",
                    recentSummaries.TakeLast(4).Select(s => $"{s.Role}: {s.Summary}"))
                : "";

            var prompt = $@"你是一个意图分类器。根据用户输入，从以下固定意图中选择最匹配的一个。

重要区分规则：
- code_generation 仅用于编写程序代码（如函数、类、算法、脚本等）
- content_creation 用于写小说、文章、故事、剧本、诗歌等创意内容
- file_read 用于读取、查看、显示文件内容
- summarization 用于总结、概括、摘要文档或文本内容

可用意图:
{intentListStr}
{summaryStr}

用户最新输入:
{userInput}

请仅返回意图的英文标识（如 code_generation），不要返回其他任何内容。";

            var response = await _llmClient.GenerateAsync(
                prompt,
                modelId,
                temperature: 0.1,  // 低温度确保分类稳定
                maxTokens: 50,     // 只需要一个标识符
                cancellationToken: cancellationToken);

            var intent = response.Trim().ToLowerInvariant();

            // 如果返回的标识不在枚举中，回退到 general_chat
            if (!IntentEnums.ContainsKey(intent))
            {
                // 尝试从响应中提取有效标识
                intent = IntentEnums.Keys.FirstOrDefault(k =>
                    response.Contains(k, StringComparison.OrdinalIgnoreCase)) ?? "general_chat";
            }

            return new IntentClassificationResult
            {
                Intent = intent,
                Description = IntentEnums.GetValueOrDefault(intent, "一般对话"),
                Confidence = 0.8
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "意图分类失败，回退到 general_chat");
            return new IntentClassificationResult
            {
                Intent = "general_chat",
                Description = "一般对话",
                Confidence = 0.5
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IntentInferenceResult> InferIntentAsync(
        string userInput,
        Guid projectId,
        string sessionKey,
        List<ChatMessageDto> shortTermMessages,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 构建短期对话记忆（近 3 轮 = 6 条消息）
            var shortTermStr = BuildShortTermContext(shortTermMessages);

            // 2. 长期知识库轻量检索
            var knowledgeSnippets = await SearchKnowledgeBase(projectId, userInput, cancellationToken);
            var knowledgeStr = knowledgeSnippets.Count > 0
                ? "\n相关知识片段:\n" + string.Join("\n", knowledgeSnippets.Select((s, i) => $"[{i + 1}] {s}"))
                : "";

            // 3. 获取全部可用 Tools 完整描述
            var tools = await _toolDefRepo.GetEnabledAsync(cancellationToken);
            var toolsStr = string.Join("\n", tools.Select(t =>
                $"- {t.Name} ({t.Category}): {t.Description}"));

            // 4. 获取全部可用 Skills 完整描述
            var skills = await _skillRepo.GetEnabledAsync(cancellationToken);
            var skillsStr = string.Join("\n", skills.Select(s =>
                $"- {s.Name} ({s.Category}): {s.Description}"));

            // 5. 构建意图枚举
            var intentListStr = string.Join("\n",
                IntentEnums.Select(kv => $"- {kv.Key}: {kv.Value}"));

            var prompt = $@"你是一个智能意图推测器。根据用户输入、对话上下文、可用工具和技能，推测用户的真实意图。

## 可用意图
{intentListStr}

## 短期对话记忆（最近 3 轮）
{shortTermStr}

## 长期知识库
{knowledgeStr}

## 可用工具 (Tools)
{toolsStr}

## 可用技能 (Skills)
{skillsStr}

## 用户最新输入
{userInput}

请以 JSON 格式返回推测结果，包含以下字段：
{{
  ""intent"": ""意图英文标识"",
  ""description"": ""意图中文描述"",
  ""reasoning"": ""推测依据的简短说明"",
  ""recommendedTools"": [""推荐工具名称""],
  ""recommendedSkills"": [""推荐技能名称""],
  ""confidence"": 0.85
}}

仅返回 JSON，不要其他内容。";

            var response = await _llmClient.GenerateAsync(
                prompt,
                modelId,
                temperature: 0.3,
                maxTokens: 500,
                cancellationToken: cancellationToken);

            // 解析 JSON 响应
            var result = ParseInferenceResponse(response);
            result.KnowledgeSnippets = knowledgeSnippets;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "意图推测失败，回退到 general_chat");
            return new IntentInferenceResult
            {
                Intent = "general_chat",
                Description = "一般对话",
                Reasoning = "推测过程异常",
                Confidence = 0.3
            };
        }
    }

    /// <summary>
    /// 构建短期对话上下文
    /// </summary>
    private static string BuildShortTermContext(List<ChatMessageDto> messages)
    {
        if (messages.Count == 0)
            return "(无历史对话)";

        // 取最近 3 轮 = 6 条消息
        var recent = messages.TakeLast(6).ToList();
        return string.Join("\n", recent.Select(m =>
            $"{m.Role}: {TruncateText(m.Content, 200)}"));
    }

    /// <summary>
    /// 长期知识库轻量检索（只取少量相关片段）
    /// </summary>
    private async Task<List<string>> SearchKnowledgeBase(
        Guid projectId,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var memories = await _memorySystem.SearchProjectLongTermMemoryAsync(
                projectId, query, limit: 3, cancellationToken);

            return memories
                .Where(m => !string.IsNullOrWhiteSpace(m.Summary))
                .Select(m => TruncateText(m.Summary, 150))
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 解析意图推测的 JSON 响应
    /// </summary>
    private IntentInferenceResult ParseInferenceResponse(string response)
    {
        try
        {
            // 尝试提取 JSON 块
            var jsonStr = ExtractJsonFromResponse(response);
            if (string.IsNullOrEmpty(jsonStr))
                return FallbackResult(response);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr, options);

            var intent = json.TryGetProperty("intent", out var intentProp)
                ? intentProp.GetString() ?? "general_chat"
                : "general_chat";

            // 验证 intent 是否在枚举中
            if (!IntentEnums.ContainsKey(intent))
                intent = "general_chat";

            var result = new IntentInferenceResult
            {
                Intent = intent,
                Description = json.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? IntentEnums.GetValueOrDefault(intent, "一般对话")
                    : IntentEnums.GetValueOrDefault(intent, "一般对话"),
                Reasoning = json.TryGetProperty("reasoning", out var reasonProp)
                    ? reasonProp.GetString() ?? ""
                    : "",
                Confidence = json.TryGetProperty("confidence", out var confProp)
                    ? confProp.GetDouble()
                    : 0.7
            };

            // 解析推荐工具
            if (json.TryGetProperty("recommendedTools", out var toolsProp) && toolsProp.ValueKind == JsonValueKind.Array)
            {
                result.RecommendedTools = toolsProp.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            // 解析推荐技能
            if (json.TryGetProperty("recommendedSkills", out var skillsProp) && skillsProp.ValueKind == JsonValueKind.Array)
            {
                result.RecommendedSkills = skillsProp.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            return result;
        }
        catch
        {
            return FallbackResult(response);
        }
    }

    /// <summary>
    /// 从 LLM 响应中提取 JSON 字符串
    /// </summary>
    private static string? ExtractJsonFromResponse(string response)
    {
        var trimmed = response.Trim();

        // 尝试直接解析
        if (trimmed.StartsWith("{"))
            return trimmed;

        // 尝试从 markdown 代码块中提取
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
            return trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);

        return null;
    }

    private static IntentInferenceResult FallbackResult(string rawResponse)
    {
        return new IntentInferenceResult
        {
            Intent = "general_chat",
            Description = "一般对话",
            Reasoning = $"无法解析推测结果: {TruncateText(rawResponse, 100)}",
            Confidence = 0.5
        };
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
