using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IAuditLogWriter
    {
        // Chỉ thêm AuditLog vào DbContext.
        // Service nghiệp vụ gọi SaveChangesAsync sau đó để thay đổi
        // nghiệp vụ và AuditLog được lưu trong cùng một transaction.
        Task WriteAsync(
            int? actorUserId,
            AuditActionType actionType,
            string entityName,
            int entityId,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken cancellationToken = default);
    }

}
