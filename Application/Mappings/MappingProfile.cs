using Application.DTOs.Auth;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, AuthResponseDTO>();

            CreateMap<User, UserDTO>()
                .ForMember(
                    destination =>
                        destination.RoleName,
                    option => option.MapFrom(
                        source =>
                            source.Role != null
                                ? source.Role.RoleName.ToString()
                                : string.Empty))
                .ForMember(
                    destination =>
                        destination.DepartmentName,
                    option => option.MapFrom(
                        source =>
                            source.Department != null
                                ? source.Department.DepartmentName
                                : string.Empty));
        }
    }
}
