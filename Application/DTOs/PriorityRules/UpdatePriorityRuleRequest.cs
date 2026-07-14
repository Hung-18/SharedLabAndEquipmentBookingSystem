using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PriorityRules
{
    public class UpdatePriorityRuleRequest
    {
        // Chỉ Admin được cập nhật quy tắc ưu tiên.
        public int ActorUserId { get; set; }

        public int PriorityLevel { get; set; }

        public string? Description { get; set; }
    }

}
