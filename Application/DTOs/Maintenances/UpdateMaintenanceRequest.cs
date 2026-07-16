using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Maintenances
{
    public class UpdateMaintenanceRequest
    {
        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal MaintenanceCost { get; set; }

        public string? Notes { get; set; }

        public MaintenanceRecurrenceType RecurrenceType { get; set; }
            = MaintenanceRecurrenceType.None;

        public int RecurrenceInterval { get; set; } = 1;

        public DateTime? RecurrenceEndDate { get; set; }
    }
}
