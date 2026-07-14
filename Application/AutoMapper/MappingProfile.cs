using Application.DTOs;
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
            //map user sang authresponsedto
            CreateMap<User, AuthResponseDTO>();
            //map user sang userDTO
            CreateMap<User, UserDTO>();

            
        }
    }
}
