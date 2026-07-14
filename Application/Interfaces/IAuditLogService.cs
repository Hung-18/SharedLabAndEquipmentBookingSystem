using Application.DTOs.AuditLogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAuditLogService
    {
        Task<PagedAuditLogResponse> SearchAsync(
            AuditLogQueryRequest request,
            CancellationToken cancellationToken);

        Task<AuditLogResponse?> GetByIdAsync(
            int id,
            int? actorUserId,
            CancellationToken cancellationToken);
    }

}
