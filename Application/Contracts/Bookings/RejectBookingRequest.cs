using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class RejectBookingRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
    }

}
