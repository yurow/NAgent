namespace NAgent.Domain.Events;

public record UserCreatedEvent(Guid UserId, string Username, string Email) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
