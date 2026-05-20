using MediatR;

namespace NAgent.AgentDomain.Events;

/// <summary>
/// 领域事件接口
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
