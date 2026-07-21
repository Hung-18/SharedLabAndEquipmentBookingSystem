using Application.DTOs.AuditLogs;

namespace Application.Interfaces
{
    public interface IAuditLogService
    {
        Task<PagedAuditLogResponse> SearchAsync(
            AuditLogQueryRequest request,
            CancellationToken cancellationToken);

        Task<AuditLogResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);
    }
}
