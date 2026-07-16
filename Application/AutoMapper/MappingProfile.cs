using Application.DTOs.Auth;
using Application.DTOs.Booking;
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
    //        //map booking entoty sang bookingresponse
    //        CreateMap<Booking, BookingResponse>()
    //.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
    //.ForMember(dest => dest.Lab, opt => opt.MapFrom(src => src.BookingItems.Select(x => x.LabRoom.LabName).ToList()))
    //.ForMember(dest => dest.EquipmentNames, opt => opt.MapFrom(src => src.BookingItems.Select(x => x.Equipment.EquipmentName).ToList()));

        }
    }
}
