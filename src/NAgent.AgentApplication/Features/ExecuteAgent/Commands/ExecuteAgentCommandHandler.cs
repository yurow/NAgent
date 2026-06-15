using MediatR;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;
using NAgent.AgentDomain.Services.Tools;

namespace NAgent.AgentApplication.Features.ExecuteAgent.Commands;

/// <summary>
/// 执行 Agent 命令处理器
/// </summary>
public class ExecuteAgentCommandHandler : IRequestHandler<ExecuteAgentCommand, ExecuteAgentResult>
{
    private readonly IAgentEngine _agentEngine;
    private readonly IAgentSessionRepository _sessionRepository;
    private readonly IPromptFilter _promptFilter;
    private readonly ISandboxResultValidator _resultValidator;
    private readonly IProjectRepository _projectRepository;
    private readonly ILlmModelRepository _llmModelRepository;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILlmClient _llmClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly ISkillRepository _skillRepository;
    private readonly IToolDefinitionRepository _toolDefinitionRepository;

    public ExecuteAgentCommandHandler(
        IAgentEngine agentEngine,
        IAgentSessionRepository sessionRepository,
        IPromptFilter promptFilter,
        ISandboxResultValidator resultValidator,
        IProjectRepository projectRepository,
        ILlmModelRepository llmModelRepository,
        IWorkspaceManager workspaceManager,
        ILlmClient llmClient,
        IToolRegistry toolRegistry,
        ISkillRepository skillRepository,
        IToolDefinitionRepository toolDefinitionRepository)
    {
        _agentEngine = agentEngine ?? throw new ArgumentNullException(nameof(agentEngine));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _promptFilter = promptFilter ?? throw new ArgumentNullException(nameof(promptFilter));
        _resultValidator = resultValidator ?? throw new ArgumentNullException(nameof(resultValidator));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _llmModelRepository = llmModelRepository ?? throw new ArgumentNullException(nameof(llmModelRepository));
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _toolDefinitionRepository = toolDefinitionRepository ?? throw new ArgumentNullException(nameof(toolDefinitionRepository));
    }

    public async Task<ExecuteAgentResult> Handle(ExecuteAgentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 验证项目是否存在
            var projectId = Guid.Parse(request.ProjectId);
            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                return new ExecuteAgentResult(false, "项目不存在");
            }

            // 2. 安全过滤
            var filterResult = _promptFilter.Filter(request.UserInput);
            if (!filterResult.IsSafe)
            {
                return new ExecuteAgentResult(false, $"安全拦截: {filterResult.Warning}");
            }

            var userId = request.UserId;

            // 3. 确保工作目录存在
            var workspacePath = _workspaceManager.EnsureProjectWorkspace(userId, projectId);

            // 4. 检查是否已初始化
            var isInitialized = _workspaceManager.IsInitialized(userId, projectId);

            if (!isInitialized)
            {
                // 首次对话：执行项目初始化流程
                return await HandleInitializationAsync(
                    request, userId, projectId, project, workspacePath, 
                    filterResult.CleanedInput, cancellationToken);
            }

            // 5. 已初始化：正常对话流程
            return await HandleNormalChatAsync(
                request, userId, projectId, workspacePath, 
                filterResult.CleanedInput, cancellationToken);
        }
        catch (Exception ex)
        {
            return new ExecuteAgentResult(false, $"执行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 项目初始化流程
    /// </summary>
    private async Task<ExecuteAgentResult> HandleInitializationAsync(
        ExecuteAgentCommand request, Guid userId, Guid projectId, 
        AgentDomain.Entities.Project project, string workspacePath, string userInput,
        CancellationToken cancellationToken)
    {
        // 1. 创建 init.md 标记初始化
        _workspaceManager.EnsureInitFile(userId, projectId, project.Name);

        // 2. 获取 skills 和 tools 列表
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        var tools = _toolRegistry.GetAllTools().ToList();
        var yamlTools = await _toolDefinitionRepository.GetAllAsync(cancellationToken);

        var skillsDesc = string.Join("\n", skills.Select(s => $"- {s.Name}: {s.Description} (分类: {s.Category})"));
        var builtInToolsDesc = string.Join("\n", tools.Select(t => $"- {t.ToolName}: {t.Description}"));
        var yamlToolsDesc = string.Join("\n", yamlTools.Select(t => $"- {t.Name}: {t.Description} (分类: {t.Category})"));

        // 3. 构建初始化 Prompt，让模型推测项目用途
        var initPrompt = $@"你是一个 AI 项目助手。用户正在创建一个新项目，请根据用户描述推测项目的用途、可能用到的工具和技能。

项目名称: {project.Name}
项目描述: {project.Description ?? "无"}
工作目录: {workspacePath}
用户首次输入: {userInput}

当前系统可用的 Skills:
{skillsDesc}

当前系统可用的内置 Tools:
{builtInToolsDesc}

当前系统可用的配置 Tools (YAML):
{yamlToolsDesc}

请基于以上信息，分析并推测：

1. **项目用途**: 这个项目最可能的用途是什么？
2. **推荐 Tools**: 列出该项目可能用到的工具及其用途
3. **推荐 Skills**: 列出该项目可能用到的技能及其用途
4. **项目规划**: 简要描述项目的初步规划

请用 Markdown 格式输出，不要包含多余的说明文字。直接输出以下格式：

## 项目用途
[推测的项目用途]

## 推荐 Tools
- 工具名: 用途说明

## 推荐 Skills
- 技能名: 用途说明

## 项目规划
[初步规划]";

        // 4. 调用 LLM 推测项目用途
        var analysisResult = await _llmClient.GenerateAsync(initPrompt, request.ModelId, cancellationToken: cancellationToken);

        // 5. 生成 spec.md 内容
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var specContent = $@"# 项目规范文档 (spec.md)

**项目名称**: {project.Name}
**项目ID**: {projectId}
**创建时间**: {timestamp} UTC
**工作目录**: {workspacePath}

---

## 项目用途

{analysisResult}

---

## 对话记录

本文档记录用户与 AI Agent 的所有对话，用于追踪项目需求和流程。

### [{timestamp}] 项目初始化

**用户输入**: {userInput}

**系统分析**: 已完成项目用途推测和工具/技能推荐。

---

";

        // 6. 写入 spec.md
        _workspaceManager.WriteSpecFile(userId, projectId, specContent);

        // 7. 构建友好的初始化回复
        var reply = $"项目工作目录已创建：`{workspacePath}`\n\n" +
                   $"已为你初始化项目 **{project.Name}**，并完成了项目分析：\n\n" +
                   $"---\n\n" +
                   $"{analysisResult}\n\n" +
                   $"---\n\n" +
                   $"以上分析已保存到 `spec.md` 文件中。你可以随时查看和修改。\n" +
                   $"现在你可以开始使用这个项目了！有什么需要我帮忙的吗？";

        // 8. 获取模型名称
        string? modelName = null;
        if (!string.IsNullOrEmpty(request.ModelId))
        {
            var model = await _llmModelRepository.GetByIdAsync(Guid.Parse(request.ModelId), cancellationToken);
            modelName = model?.DisplayName ?? request.ModelId;
        }

        return new ExecuteAgentResult(true, reply, null, modelName);
    }

    /// <summary>
    /// 正常对话流程
    /// </summary>
    private async Task<ExecuteAgentResult> HandleNormalChatAsync(
        ExecuteAgentCommand request, Guid userId, Guid projectId, string workspacePath,
        string userInput, CancellationToken cancellationToken)
    {
        // 1. 加载或创建会话
        var session = await GetOrCreateSessionAsync(request.SessionId, projectId, cancellationToken);

        // 2. 设置工作目录上下文变量
        session.SetContextVariable("workspace_path", workspacePath);
        var specContent = _workspaceManager.ReadSpecFile(userId, projectId);
        session.SetContextVariable("spec_content", specContent);
        var files = _workspaceManager.GetWorkspaceFiles(userId, projectId);
        session.SetContextVariable("workspace_files", string.Join("\n", files));

        // 3. 添加用户消息
        session.AddUserMessage(userInput);

        // 4. 执行 Agent
        var executionResult = await _agentEngine.ExecuteAsync(
            session,
            userInput,
            request.ModelId,
            cancellationToken);

        // 5. 如果使用了工具，校验结果
        if (executionResult.ToolName != null && !executionResult.Success)
        {
            var validation = _resultValidator.Validate(executionResult.Output);
            if (!validation.IsPassed)
            {
                return new ExecuteAgentResult(false, $"结果校验失败: {string.Join("; ", validation.Warnings)}");
            }
        }

        // 6. 添加助手消息
        session.AddAssistantMessage(executionResult.Output);

        // 7. 保存会话
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // 8. 将用户问题和 AI 回复追加到 spec.md
        _workspaceManager.AppendQuestionToSpec(userId, projectId, userInput, executionResult.Output);

        // 9. 获取模型名称
        string? modelName = executionResult.ModelName;
        if (string.IsNullOrEmpty(modelName) && !string.IsNullOrEmpty(request.ModelId))
        {
            var model = await _llmModelRepository.GetByIdAsync(Guid.Parse(request.ModelId), cancellationToken);
            modelName = model?.DisplayName ?? request.ModelId;
        }

        return new ExecuteAgentResult(
            executionResult.Success,
            executionResult.Output,
            null,
            modelName,
            executionResult.Metadata);
    }

    private async Task<AgentSession> GetOrCreateSessionAsync(string sessionKey, Guid projectId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetBySessionKeyAsync(sessionKey, cancellationToken);

        if (session == null)
        {
            session = new AgentSession(sessionKey, projectId);
            await _sessionRepository.AddAsync(session, cancellationToken);
        }

        return session;
    }
}
