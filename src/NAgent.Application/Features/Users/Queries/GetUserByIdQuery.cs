using MediatR;
using NAgent.Application.DTOs;

namespace NAgent.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;
