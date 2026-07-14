using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PriorityRules
{
    public class PriorityRuleResponse
    {
        public int PriorityRuleId { get; set; }

        public string PurposeType { get; set; } = string.Empty;

        public int PriorityLevel { get; set; }

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;
    }

}
