using Domain;

namespace Application.DTOs.Reports
{
    public class MaintenanceHistoryQueryRequest
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public MaintenanceStatus? Status { get; set; }

        // Khi lọc theo LabId, báo cáo gồm cả maintenance trực tiếp
        // của phòng và maintenance của thiết bị thuộc phòng đó.
        public int? LabId { get; set; }

        public int? EquipmentId { get; set; }

        public int? CreatedById { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
