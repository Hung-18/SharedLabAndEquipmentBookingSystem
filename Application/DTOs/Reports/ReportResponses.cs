namespace Application.DTOs.Reports
{
    public class ResourceUtilizationResponse
    {
        public string ResourceType { get; set; } = string.Empty;
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public int? LabId { get; set; }
        public string? LabName { get; set; }
        public int BookingCount { get; set; }
        public double ReservedHours { get; set; }
        public int UsageCount { get; set; }
        public double ActualUsageHours { get; set; }
        public double AvailableHours { get; set; }
        public double UtilizationRate { get; set; }
    }

    public class DepartmentUtilizationResponse
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public double ReservedHours { get; set; }
        public int UsageCount { get; set; }
        public double ActualUsageHours { get; set; }

        // Tổng giờ khả dụng thực tế của các tài nguyên mà khoa/bộ môn
        // đã đặt trong kỳ, sau khi trừ thời gian maintenance khóa tài nguyên.
        public double AvailableResourceHours { get; set; }

        // Tỷ lệ sử dụng thật = giờ sử dụng thực tế / giờ tài nguyên khả dụng.
        public double UtilizationRate { get; set; }

        // Tỷ trọng giờ sử dụng của khoa trong tổng giờ sử dụng toàn hệ thống.
        // Đây là chỉ số khác với UtilizationRate.
        public double UsageSharePercentage { get; set; }
    }

    public class CategoryCountResponse
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MaintenanceCostResponse
    {
        public string ResourceType { get; set; } = string.Empty;
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public int? LabId { get; set; }
        public string? LabName { get; set; }
        public int MaintenanceCount { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class MostUsedResourceResponse
    {
        public string ResourceType { get; set; } = string.Empty;
        public int ResourceId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public int? LabId { get; set; }
        public string? LabName { get; set; }
        public int BookingCount { get; set; }
        public double ReservedHours { get; set; }
        public int UsageCount { get; set; }
        public double ActualUsageHours { get; set; }
    }

    public class ViolationReportResponse
    {
        public int ViolationId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public int PenaltyPointsAdded { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }

    public class ViolationSummaryResponse
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int ResolvedCount { get; set; }
        public int CancelledCount { get; set; }
        public List<CategoryCountResponse> ViolationTypeCounts { get; set; } = new();
        public List<ViolationReportResponse> Items { get; set; } = new();
    }

    public class PenaltyUserReportResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int PenaltyPoints { get; set; }
        public int PenaltyPointsInPeriod { get; set; }
        public int ActiveViolationCount { get; set; }
        public int TotalViolationCount { get; set; }
        public string UserStatus { get; set; } = string.Empty;
        public DateTime? RestrictionUntil { get; set; }
    }

    public class NoShowRateResponse
    {
        public int NoShowCount { get; set; }
        public int CompletedCount { get; set; }
        public int ConcludedBookingCount { get; set; }
        public double NoShowRate { get; set; }
    }

    public class UsageTrendResponse
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int UsageCount { get; set; }
        public double TotalUsageHours { get; set; }
    }

    public class DashboardResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalBookings { get; set; }
        public int TotalUsageLogs { get; set; }
        public int TotalViolations { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public NoShowRateResponse NoShow { get; set; } = new();
        public List<CategoryCountResponse> BookingStatusCounts { get; set; } = new();
        public List<CategoryCountResponse> BookingPurposeCounts { get; set; } = new();
        public List<CategoryCountResponse> BookingDepartmentCounts { get; set; } = new();
        public List<ResourceUtilizationResponse> LabUtilization { get; set; } = new();
        public List<ResourceUtilizationResponse> EquipmentUtilization { get; set; } = new();
        public List<DepartmentUtilizationResponse> DepartmentUtilization { get; set; } = new();
        public List<MostUsedResourceResponse> MostUsedLabRooms { get; set; } = new();
        public List<MostUsedResourceResponse> MostUsedEquipments { get; set; } = new();
        public List<PenaltyUserReportResponse> UsersWithMostPenaltyPoints { get; set; } = new();
        public List<UsageTrendResponse> UsageTrend { get; set; } = new();
    }
}
