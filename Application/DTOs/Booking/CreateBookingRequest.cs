using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class CreateBookingRequest
    {
        public int UserId { get; set; }
        public BookingPurposeType PurposeType { get; set; }
        public string PurposeDescription { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<BookingItemRequest> Items { get; set; } = new();
    }

}
