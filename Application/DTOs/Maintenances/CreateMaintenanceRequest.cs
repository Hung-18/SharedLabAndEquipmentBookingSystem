using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Maintenances
{
    public class CreateMaintenanceRequest
    {
        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public int CreatedById { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal MaintenanceCost { get; set; }

        public string? Notes { get; set; }
    }
}
