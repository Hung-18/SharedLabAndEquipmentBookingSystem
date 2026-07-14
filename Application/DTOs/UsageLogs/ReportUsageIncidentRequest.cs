using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.UsageLogs
{
    public class ReportUsageIncidentRequest
    {
        public int UserId { get; set; }

        public UsageIncidentStatus IncidentStatus { get; set; }

        public string IncidentDescription { get; set; } = string.Empty;
    }
}
