namespace Application.DTOs.Reports
{
    public class MaintenanceHistoryItemResponse
    {
        public int MaintenanceId { get; set; }

        public string ResourceType { get; set; } = string.Empty;

        public int ResourceId { get; set; }

        public string ResourceName { get; set; } = string.Empty;

        public int? LabId { get; set; }

        public string? LabName { get; set; }

        public int CreatedById { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public double DurationHours { get; set; }

        public decimal MaintenanceCost { get; set; }

        public string? Notes { get; set; }

        public string Status { get; set; } = string.Empty;

        public string RecurrenceType { get; set; } = string.Empty;

        public int RecurrenceInterval { get; set; }

        public DateTime? RecurrenceEndDate { get; set; }

        public int? ParentMaintenanceId { get; set; }
    }

    public class PagedMaintenanceHistoryResponse
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        // Tổng chi phí của toàn bộ bản ghi khớp bộ lọc, không chỉ trang hiện tại.
        // Maintenance Cancelled không được tính vào tổng chi phí.
        public decimal TotalCost { get; set; }

        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public List<MaintenanceHistoryItemResponse> Items { get; set; } = new();
    }
}
