using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class AlternativeSlotRequest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<BookingItemRequest> Items { get; set; } = new();
        public int MaxSuggestions { get; set; } = 3;
        public int SearchDays { get; set; } = 14;
        public int StepMinutes { get; set; } = 30;
    }

}
