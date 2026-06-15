using MediatR;
using NAgent.AgentApplication.Interfaces;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

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

    public ExecuteAgentCommandHandler(
        IAgentEngine agentEngine,
        IAgentSessionRepository sessionRepository,
        IPromptFilter promptFilter,
        ISandboxResultValidator resultValidator,
        IProjectRepository projectRepository,
        ILlmModelRepository llmModelRepository)
    {
        _agentEngine = agentEngine ?? throw new ArgumentNullException(nameof(agentEngine));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _promptFilter = promptFilter ?? throw new ArgumentNullException(nameof(promptFilter));
        _resultValidator = resultValidator ?? throw new ArgumentNullException(nameof(resultValidator));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _llmModelRepository = llmModelRepository ?? throw new ArgumentNullException(nameof(llmModelRepository));
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

            // 3. 加载或创建会话
            var session = await GetOrCreateSessionAsync(request.SessionId, projectId, cancellationToken);
            
            // 4. 添加用户消息
            session.AddUserMessage(filterResult.CleanedInput);

            // 5. 执行 Agent（传入模型ID）
            var executionResult = await _agentEngine.ExecuteAsync(
                session, 
                filterResult.CleanedInput, 
                request.ModelId,
                cancellationToken);

            // 6. 如果使用了工具，校验结果
            if (executionResult.ToolName != null && !executionResult.Success)
            {
                var validation = _resultValidator.Validate(executionResult.Output);
                if (!validation.IsPassed)
                {
                    return new ExecuteAgentResult(false, $"结果校验失败: {string.Join("; ", validation.Warnings)}");
                }
            }

            // 7. 添加助手消息
            session.AddAssistantMessage(executionResult.Output);

            // 8. 保存会话
            await _sessionRepository.UpdateAsync(session, cancellationToken);

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
        catch (Exception ex)
        {
            return new ExecuteAgentResult(false, $"执行异常: {ex.Message}");
        }
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