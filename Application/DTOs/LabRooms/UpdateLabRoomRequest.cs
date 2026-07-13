using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.LabRooms
{
    public class UpdateLabRoomRequest
    {
        public string LabName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? UsageGuideline { get; set; }
    }
}
