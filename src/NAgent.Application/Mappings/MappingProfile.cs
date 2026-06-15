using AutoMapper;
using NAgent.Application.DTOs;
using NAgent.Domain.Entities;

namespace NAgent.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.isActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.isAdmin, opt => opt.MapFrom(src => src.IsAdmin))
            .ForMember(dest => dest.createdAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.updatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
    }
}
