using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.UsageLogs
{
    public class CheckOutUsageRequest
    {
        // Production clients should leave this null. The server uses DateTime.UtcNow.
        // Only Admin/LabManager may provide a historical value for data correction.
        public DateTime? ActualCheckout { get; set; }
    }

}
