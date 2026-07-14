using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IAuditLogRepository
        : IBaseRepository<AuditLog>
    {
        Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
            int userId,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
            string entityName,
            int entityId,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<AuditLog> Items, int TotalCount)>
            SearchAsync(
                int? userId,
                AuditActionType? actionType,
                string? entityName,
                int? entityId,
                DateTime? from,
                DateTime? to,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default);
    }


}
