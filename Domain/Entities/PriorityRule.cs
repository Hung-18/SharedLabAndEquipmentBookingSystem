

namespace Domain.Entities
{
    public class PriorityRule
    {
        protected PriorityRule()
        {
        }

        public PriorityRule(
            BookingPurposeType purposeType,
            int priorityLevel,
            string? description = null)
        {
            ValidatePurposeType(purposeType);
            ValidatePriorityLevel(priorityLevel);
            ValidateDescription(description);

            PurposeType = purposeType;
            PriorityLevel = priorityLevel;
            Description = description?.Trim();
            Status = PriorityRuleStatus.Active;
        }

        public int PriorityRuleId { get; private set; }

        public BookingPurposeType PurposeType { get; private set; }

        public int PriorityLevel { get; private set; }

        public string? Description { get; private set; }

        public PriorityRuleStatus Status { get; private set; }
            = PriorityRuleStatus.Active;

        public ICollection<Booking> Bookings { get; private set; }
            = new List<Booking>();

        public void UpdateDetails(
            int priorityLevel,
            string? description)
        {
            ValidatePriorityLevel(priorityLevel);
            ValidateDescription(description);

            PriorityLevel = priorityLevel;
            Description = description?.Trim();
        }

        public void Activate()
        {
            if (Status == PriorityRuleStatus.Active)
            {
                return;
            }

            Status = PriorityRuleStatus.Active;
        }

        public void Deactivate()
        {
            if (Status == PriorityRuleStatus.Inactive)
            {
                return;
            }

            Status = PriorityRuleStatus.Inactive;
        }

        private static void ValidatePurposeType(
            BookingPurposeType purposeType)
        {
            if (!Enum.IsDefined(
                    typeof(BookingPurposeType),
                    purposeType))
            {
                throw new ArgumentException(
                    "Loại mục đích đặt lịch không hợp lệ.");
            }
        }

        private static void ValidatePriorityLevel(
            int priorityLevel)
        {
            if (priorityLevel <= 0)
            {
                throw new ArgumentException(
                    "Mức độ ưu tiên phải lớn hơn 0.");
            }
        }

        private static void ValidateDescription(
            string? description)
        {
            if (description?.Trim().Length > 500)
            {
                throw new ArgumentException(
                    "Mô tả không được vượt quá 500 ký tự.");
            }
        }
    }

}