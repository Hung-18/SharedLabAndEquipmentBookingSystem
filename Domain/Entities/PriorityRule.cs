

namespace Domain.Entities
{
    public class PriorityRule
    {
        protected PriorityRule() { }

        public PriorityRule(
            BookingPurposeType purposeType,
            int priorityLevel,
            string? description = null)
        {
            if (priorityLevel <= 0)
                throw new ArgumentException("Mức độ ưu tiên phải lớn hơn 0");

            PurposeType = purposeType;
            PriorityLevel = priorityLevel;
            Description = description?.Trim();
            Status = PriorityRuleStatus.Active;
        }

        public int PriorityRuleId { get; private set; }

        public BookingPurposeType PurposeType { get; private set; }

        public int PriorityLevel { get; private set; }

        public string? Description { get; private set; }

        public PriorityRuleStatus Status { get; private set; } = PriorityRuleStatus.Active;

        public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    }
}