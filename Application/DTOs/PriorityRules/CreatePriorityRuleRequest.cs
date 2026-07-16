using Domain;

namespace Application.DTOs.PriorityRules
{
    public class CreatePriorityRuleRequest
    {
        public BookingPurposeType PurposeType { get; set; }
        public int PriorityLevel { get; set; }
        public string? Description { get; set; }
    }
}
