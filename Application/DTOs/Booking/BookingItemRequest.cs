using System;
using System.Collections.Generic;
using Domain;
using System.Text;

namespace Application.DTOs.Booking
{
    public class BookingItemRequest
    {
        public ResourceType ResourceType { get; set; }
        public int? LabId { get; set; }
        public int? EquipmentId { get; set; }
        public string? Note { get; set; }
    }

}
