using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.PriorityRules
{
    public class CreatePriorityRuleRequest
    {
        // Chỉ Admin được tạo quy tắc ưu tiên.
        public int ActorUserId { get; set; }

        public BookingPurposeType PurposeType { get; set; }

        public int PriorityLevel { get; set; }

        public string? Description { get; set; }
    }

}
