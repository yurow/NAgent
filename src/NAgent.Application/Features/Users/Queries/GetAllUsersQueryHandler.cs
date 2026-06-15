using AutoMapper;
using MediatR;
using NAgent.Application.DTOs;
using NAgent.Domain.Repositories;

namespace NAgent.Application.Features.Users.Queries;

/// <summary>
/// 获取所有用户查询处理器
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAllUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAllAsync(cancellationToken);
        return _mapper.Map<List<UserDto>>(users);
    }
}
