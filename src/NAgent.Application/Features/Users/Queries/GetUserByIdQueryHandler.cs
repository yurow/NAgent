using AutoMapper;
using MediatR;
using NAgent.Application.DTOs;
using NAgent.Domain.Exceptions;
using NAgent.Domain.Repositories;

namespace NAgent.Application.Features.Users.Queries;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new DomainException($"用户 {request.UserId} 不存在");

        return _mapper.Map<UserDto>(user);
    }
}
