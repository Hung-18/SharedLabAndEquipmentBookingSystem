using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.LabRooms
{
    public class LabRoomSearchRequest
    {
        public string? Keyword { get; set; }
        public LabRoomStatus? Status { get; set; }
        public int? ManagerId { get; set; }
        public int? MinimumCapacity { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

}
