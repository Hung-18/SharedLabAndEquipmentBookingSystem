using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string PurposeType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

}
