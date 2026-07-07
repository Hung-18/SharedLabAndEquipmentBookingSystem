

namespace Domain.Entities
{
    public class Booking
    {
        protected Booking() { }

        public Booking(
            int userId,
            int? priorityRuleId,
            BookingPurposeType purposeType,
            string purposeDescription,
            DateTime startTime,
            DateTime endTime)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(purposeDescription))
                throw new ArgumentException("Mô tả mục đích không được để trống");

            if (startTime >= endTime)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");

            UserId = userId;
            PriorityRuleId = priorityRuleId;
            PurposeType = purposeType;
            PurposeDescription = purposeDescription.Trim();
            StartTime = startTime;
            EndTime = endTime;
            Status = BookingStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public int BookingId { get; private set; }

        public int UserId { get; private set; }

        public int? PriorityRuleId { get; private set; }

        public int? ApprovedById { get; private set; }

        public BookingPurposeType PurposeType { get; private set; }

        public string PurposeDescription { get; private set; } = string.Empty;

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public BookingStatus Status { get; private set; } = BookingStatus.Pending;

        public string? RejectionReason { get; private set; }

        public DateTime? ApprovedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public User? User { get; private set; }

        public User? ApprovedBy { get; private set; }

        public PriorityRule? PriorityRule { get; private set; }

        public ICollection<BookingItem> BookingItems { get; private set; } = new List<BookingItem>();

        public ICollection<Violation> Violations { get; private set; } = new List<Violation>();

        public void Approve(int approvedById)
        {
            if (approvedById <= 0)
                throw new ArgumentException("ApprovedById phải lớn hơn 0");

            ApprovedById = approvedById;
            ApprovedAt = DateTime.UtcNow;
            Status = BookingStatus.Approved;
            RejectionReason = null;
        }

        public void Reject(int approvedById, string rejectionReason)
        {
            if (approvedById <= 0)
                throw new ArgumentException("ApprovedById phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new ArgumentException("Lý do từ chối không được để trống");

            ApprovedById = approvedById;
            RejectionReason = rejectionReason.Trim();
            Status = BookingStatus.Rejected;
        }

        public void Cancel()
        {
            Status = BookingStatus.Cancelled;
        }

        public void Complete()
        {
            Status = BookingStatus.Completed;
        }

        public void MarkNoShow()
        {
            Status = BookingStatus.NoShow;
        }
    }
}