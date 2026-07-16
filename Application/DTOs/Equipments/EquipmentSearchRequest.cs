using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Equipments
{
    public class EquipmentSearchRequest
    {
        public string? Keyword { get; set; }
        public int? LabId { get; set; }
        public EquipmentStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

}
