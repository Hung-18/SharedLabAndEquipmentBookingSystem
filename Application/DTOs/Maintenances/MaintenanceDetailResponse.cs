using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Maintenances
{
    public class MaintenanceDetailResponse
    {
        public int MaintenanceId { get; set; }

        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public int CreatedById { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal MaintenanceCost { get; set; }

        public string? Notes { get; set; }

        public string Status { get; set; } = string.Empty;

        public string RecurrenceType { get; set; } = string.Empty;

        public int RecurrenceInterval { get; set; }

        public DateTime? RecurrenceEndDate { get; set; }

        public int? ParentMaintenanceId { get; set; }
    }

}
