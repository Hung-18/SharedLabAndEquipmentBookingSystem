using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Violations
{
    public class UserViolationSummaryResponse
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int PenaltyPoints { get; set; }

        public string UserStatus { get; set; } = string.Empty;

        public DateTime? RestrictionUntil { get; set; }

        public int ActiveViolationCount { get; set; }

        public int ActivePenaltyPoints { get; set; }

        public List<ViolationResponse> ActiveViolations { get; set; }
            = new();
    }

}
