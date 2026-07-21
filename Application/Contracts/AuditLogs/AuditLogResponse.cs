using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.AuditLogs
{
    public class AuditLogResponse
    {
        public int AuditLogId { get; set; }

        public int UserId { get; set; }

        public string? UserName { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
