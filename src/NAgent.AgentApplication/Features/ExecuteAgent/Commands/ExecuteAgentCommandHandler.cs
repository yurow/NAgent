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

    public ExecuteAgentCommandHandler(
        IAgentEngine agentEngine,
        IAgentSessionRepository sessionRepository,
        IPromptFilter promptFilter,
        ISandboxResultValidator resultValidator)
    {
        _agentEngine = agentEngine ?? throw new ArgumentNullException(nameof(agentEngine));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _promptFilter = promptFilter ?? throw new ArgumentNullException(nameof(promptFilter));
        _resultValidator = resultValidator ?? throw new ArgumentNullException(nameof(resultValidator));
    }

    public async Task<ExecuteAgentResult> Handle(ExecuteAgentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 安全过滤
            var filterResult = _promptFilter.Filter(request.UserInput);
            if (!filterResult.IsSafe)
            {
                return new ExecuteAgentResult(false, $"安全拦截: {filterResult.Warning}");
            }

            // 2. 加载或创建会话
            var session = await GetOrCreateSessionAsync(request.SessionId, cancellationToken);
            
            // 3. 添加用户消息
            session.AddUserMessage(filterResult.CleanedInput);

            // 4. 执行 Agent（传入模型ID）
            var executionResult = await _agentEngine.ExecuteAsync(
                session, 
                filterResult.CleanedInput, 
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

            return new ExecuteAgentResult(
                executionResult.Success, 
                executionResult.Output, 
                executionResult.Metadata);
        }
        catch (Exception ex)
        {
            return new ExecuteAgentResult(false, $"执行异常: {ex.Message}");
        }
    }

    private async Task<AgentSession> GetOrCreateSessionAsync(string sessionKey, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetBySessionKeyAsync(sessionKey, cancellationToken);
        
        if (session == null)
        {
            session = new AgentSession(sessionKey);
            await _sessionRepository.AddAsync(session, cancellationToken);
        }

        return session;
    }
}