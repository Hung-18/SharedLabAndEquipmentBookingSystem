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
            ValidateDetails(userId, purposeType, purposeDescription, startTime, endTime);

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

        public void AddLabRoom(int labId, string? note = null)
        {
            EnsurePending();
            BookingItems.Add(new BookingItem(ResourceType.LabRoom, labId, null, note));
        }

        public void AddEquipment(int equipmentId, string? note = null)
        {
            EnsurePending();
            BookingItems.Add(new BookingItem(ResourceType.Equipment, null, equipmentId, note));
        }

        public void UpdateDetails(
            int? priorityRuleId,
            BookingPurposeType purposeType,
            string purposeDescription,
            DateTime startTime,
            DateTime endTime)
        {
            EnsurePending();
            ValidateDetails(UserId, purposeType, purposeDescription, startTime, endTime);

            PriorityRuleId = priorityRuleId;
            PurposeType = purposeType;
            PurposeDescription = purposeDescription.Trim();
            StartTime = startTime;
            EndTime = endTime;
        }

        public void Approve(int approvedById)
        {
            EnsurePending();

            if (approvedById <= 0)
                throw new ArgumentException("ApprovedById phải lớn hơn 0");

            ApprovedById = approvedById;
            ApprovedAt = DateTime.UtcNow;
            Status = BookingStatus.Approved;
            RejectionReason = null;
        }

        public void Reject(int approvedById, string rejectionReason)
        {
            EnsurePending();

            if (approvedById <= 0)
                throw new ArgumentException("ApprovedById phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new ArgumentException("Lý do từ chối không được để trống");

            ApprovedById = approvedById;
            ApprovedAt = DateTime.UtcNow;
            RejectionReason = rejectionReason.Trim();
            Status = BookingStatus.Rejected;
        }

        public void Cancel()
        {
            if (Status != BookingStatus.Pending && Status != BookingStatus.Approved)
                throw new InvalidOperationException("Chỉ booking Pending hoặc Approved mới được hủy.");

            Status = BookingStatus.Cancelled;
        }

        public void Complete()
        {
            if (Status != BookingStatus.Approved)
                throw new InvalidOperationException("Chỉ booking Approved mới được hoàn thành.");

            Status = BookingStatus.Completed;
        }

        public void MarkNoShow()
        {
            if (Status != BookingStatus.Approved)
                throw new InvalidOperationException("Chỉ booking Approved mới được đánh dấu NoShow.");

            Status = BookingStatus.NoShow;
        }

        private void EnsurePending()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException("Chỉ booking Pending mới được thực hiện thao tác này.");
        }

        private static void ValidateDetails(
            int userId,
            BookingPurposeType purposeType,
            string purposeDescription,
            DateTime startTime,
            DateTime endTime)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (!Enum.IsDefined(purposeType))
                throw new ArgumentException("Loại mục đích đặt lịch không hợp lệ.");

            if (string.IsNullOrWhiteSpace(purposeDescription))
                throw new ArgumentException("Mô tả mục đích không được để trống");

            if (startTime >= endTime)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
        }
    }
}
