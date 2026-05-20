using MediatR;

namespace NAgent.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
