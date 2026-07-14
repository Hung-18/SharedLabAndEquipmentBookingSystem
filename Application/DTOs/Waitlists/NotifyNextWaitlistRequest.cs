
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Waitlists
{
    public class NotifyNextWaitlistRequest
    {
        public int ActorUserId { get; set; }

        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public DateTime RequestedStart { get; set; }

        public DateTime RequestedEnd { get; set; }
    }
}
