

namespace Domain.Entities
{
    public class Waitlist
    {
        protected Waitlist()
        {
        }

        public Waitlist(
            int userId,
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            int queuePosition)
        {
            Validate(
                userId,
                labId,
                equipmentId,
                requestedStart,
                requestedEnd,
                queuePosition);

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

        public WaitlistStatus Status { get; private set; }
            = WaitlistStatus.Waiting;

        public User? User { get; private set; }

        public LabRoom? LabRoom { get; private set; }

        public Equipment? Equipment { get; private set; }

        public void MarkNotified()
        {
            if (Status != WaitlistStatus.Waiting)
            {
                throw new InvalidOperationException(
                    "Chỉ bản ghi đang Waiting mới được thông báo.");
            }

            NotifiedAt = DateTime.UtcNow;
            Status = WaitlistStatus.Notified;
        }

        public void MarkBooked()
        {
            if (Status != WaitlistStatus.Notified)
            {
                throw new InvalidOperationException(
                    "Chỉ bản ghi đã được thông báo mới được chuyển sang Booked.");
            }

            Status = WaitlistStatus.Booked;
        }

        public void Cancel()
        {
            if (Status == WaitlistStatus.Cancelled)
            {
                return;
            }

            if (Status == WaitlistStatus.Booked)
            {
                throw new InvalidOperationException(
                    "Không thể hủy hàng đợi đã chuyển thành booking.");
            }

            if (Status == WaitlistStatus.Expired)
            {
                throw new InvalidOperationException(
                    "Không thể hủy hàng đợi đã hết hạn.");
            }

            Status = WaitlistStatus.Cancelled;
        }

        public void Expire()
        {
            if (Status == WaitlistStatus.Expired)
            {
                return;
            }

            if (Status == WaitlistStatus.Booked)
            {
                throw new InvalidOperationException(
                    "Không thể làm hết hạn hàng đợi đã chuyển thành booking.");
            }

            if (Status == WaitlistStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Không thể làm hết hạn hàng đợi đã bị hủy.");
            }

            Status = WaitlistStatus.Expired;
        }

        private static void Validate(
            int userId,
            int? labId,
            int? equipmentId,
            DateTime requestedStart,
            DateTime requestedEnd,
            int queuePosition)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "UserId phải lớn hơn 0.");
            }

            // Phải chọn đúng một tài nguyên.
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: LabId hoặc EquipmentId.");
            }

            if (labId.HasValue && labId.Value <= 0)
            {
                throw new ArgumentException(
                    "LabId phải lớn hơn 0.");
            }

            if (equipmentId.HasValue && equipmentId.Value <= 0)
            {
                throw new ArgumentException(
                    "EquipmentId phải lớn hơn 0.");
            }

            if (requestedStart >= requestedEnd)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            if (queuePosition <= 0)
            {
                throw new ArgumentException(
                    "Vị trí hàng đợi phải lớn hơn 0.");
            }
        }
    }

}