using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Waitlists
{
    public class WaitlistResponse
    {
        public int WaitlistId { get; set; }

        public int UserId { get; set; }

        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public DateTime RequestedStart { get; set; }

        public DateTime RequestedEnd { get; set; }

        public int QueuePosition { get; set; }

        public DateTime? NotifiedAt { get; set; }

        public string Status { get; set; } = string.Empty;
    }

}
