using Application.DTOs.Reports;

namespace Application.Interfaces
{
    public interface IReportService
    {
        Task<List<ResourceUtilizationResponse>> GetLabUtilizationAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<ResourceUtilizationResponse>> GetEquipmentUtilizationAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<CategoryCountResponse>> GetBookingsByDepartmentAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<CategoryCountResponse>> GetBookingsByPurposeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<CategoryCountResponse>> GetBookingsByStatusAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<MaintenanceCostResponse>> GetMaintenanceCostsByLabAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<MaintenanceCostResponse>> GetMaintenanceCostsByEquipmentAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<MostUsedResourceResponse>> GetMostUsedLabRoomsAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default);

        Task<List<MostUsedResourceResponse>> GetMostUsedEquipmentsAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default);

        Task<ViolationSummaryResponse> GetViolationsAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<PenaltyUserReportResponse>> GetPenaltyUsersAsync(
            DateTime from,
            DateTime to,
            int top,
            CancellationToken cancellationToken = default);

        Task<NoShowRateResponse> GetNoShowRateAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<UsageTrendResponse>> GetUsageTrendAsync(
            DateTime from,
            DateTime to,
            string groupBy,
            CancellationToken cancellationToken = default);

        Task<DashboardResponse> GetDashboardAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);
    }
}
