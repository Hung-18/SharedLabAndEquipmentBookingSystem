using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Equipments
{
    public class CreateEquipmentRequest
    {
        public int LabId { get; set; }

        public string EquipmentName { get; set; } = string.Empty;

        public string? ModelSpecs { get; set; }

        public string? ImageUrl { get; set; }

        public string? UsageGuideline { get; set; }
    }
}
