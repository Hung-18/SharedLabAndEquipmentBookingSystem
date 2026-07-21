using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.AuditLogs
{
    public class PagedAuditLogResponse
    {
        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public List<AuditLogResponse> Items { get; set; }
            = new();
    }

}
