using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.AuditLogs
{
    public class AuditLogQueryRequest
    {
        // Để null thì service lấy user từ JWT.
        public int? ActorUserId { get; set; }

        public int? UserId { get; set; }

        public AuditActionType? ActionType { get; set; }

        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

}
