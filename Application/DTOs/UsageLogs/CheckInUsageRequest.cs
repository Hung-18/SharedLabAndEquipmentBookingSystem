using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.UsageLogs
{
    public class CheckInUsageRequest
    {
        public int BookingItemId { get; set; }

        public int UserId { get; set; }

        // Để null thì hệ thống sử dụng DateTime.UtcNow.
        public DateTime? ActualCheckin { get; set; }
    }

}
