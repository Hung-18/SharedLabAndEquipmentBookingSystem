using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.UsageLogs
{
    public class UsageLogResponse
    {
        public int LogId { get; set; }

        public int BookingItemId { get; set; }

        public DateTime ActualCheckin { get; set; }

        public DateTime? ActualCheckout { get; set; }

        public string IncidentStatus { get; set; } = string.Empty;

        public string? IncidentDescription { get; set; }
    }

}
