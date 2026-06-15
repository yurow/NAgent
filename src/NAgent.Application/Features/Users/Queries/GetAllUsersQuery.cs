using MediatR;
using NAgent.Application.DTOs;

namespace NAgent.Application.Features.Users.Queries;

/// <summary>
/// 获取所有用户查询
/// </summary>
public record GetAllUsersQuery : IRequest<List<UserDto>>;
