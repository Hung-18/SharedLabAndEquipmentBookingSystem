using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IAuditLogRepository : IBaseRepository<AuditLog>
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
    }

}
