using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Violations
{
    public class ViolationResponse
    {
        public int ViolationId { get; set; }

        public int UserId { get; set; }

        public int BookingId { get; set; }

        public string ViolationType { get; set; } = string.Empty;

        public int PenaltyPointsAdded { get; set; }

        public DateTime LoggedAt { get; set; }

        public string Status { get; set; } = string.Empty;
    }

}
