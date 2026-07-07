

namespace Domain.Entities
{
    public class Waitlist
    {
        protected Waitlist() { }

        public Waitlist(
            int userId,
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            int queuePosition)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (labId == null && equipmentId == null)
                throw new ArgumentException("Phải chọn phòng lab hoặc thiết bị để vào hàng đợi");

            if (requestedStart >= requestedEnd)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");

            if (queuePosition <= 0)
                throw new ArgumentException("Vị trí hàng đợi phải lớn hơn 0");

            UserId = userId;
            LabId = labId;
            EquipmentId = equipmentId;
            RequestedStart = requestedStart;
            RequestedEnd = requestedEnd;
            QueuePosition = queuePosition;
            Status = WaitlistStatus.Waiting;
        }

        public int WaitlistId { get; private set; }

        public int UserId { get; private set; }

        public int? LabId { get; private set; }

        public int? EquipmentId { get; private set; }

        public DateTime RequestedStart { get; private set; }

        public DateTime RequestedEnd { get; private set; }

        public int QueuePosition { get; private set; }

        public DateTime? NotifiedAt { get; private set; }

        public WaitlistStatus Status { get; private set; } = WaitlistStatus.Waiting;

        public User? User { get; private set; }

        public LabRoom? LabRoom { get; private set; }

        public Equipment? Equipment { get; private set; }

        public void MarkNotified()
        {
            NotifiedAt = DateTime.UtcNow;
            Status = WaitlistStatus.Notified;
        }

        public void MarkBooked()
        {
            Status = WaitlistStatus.Booked;
        }

        public void Cancel()
        {
            Status = WaitlistStatus.Cancelled;
        }

        public void Expire()
        {
            Status = WaitlistStatus.Expired;
        }
    }
}