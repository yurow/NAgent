using MediatR;

namespace NAgent.Application.Features.Users.Commands;

public record CreateUserCommand(string Username, string Email) : IRequest<Guid>;
