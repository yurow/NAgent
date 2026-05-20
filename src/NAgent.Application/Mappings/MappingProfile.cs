using AutoMapper;
using NAgent.Application.DTOs;
using NAgent.Domain.Entities;

namespace NAgent.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
