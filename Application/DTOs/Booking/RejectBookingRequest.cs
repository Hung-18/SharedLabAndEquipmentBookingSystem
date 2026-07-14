using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class RejectBookingRequest
    {
        public int UserId { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }

}
