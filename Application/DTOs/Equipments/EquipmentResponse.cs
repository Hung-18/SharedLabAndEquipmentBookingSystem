using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Equipments
{
    public class EquipmentResponse
    {
        public int EquipmentId { get; set; }

        public int LabId { get; set; }

        public string EquipmentName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
