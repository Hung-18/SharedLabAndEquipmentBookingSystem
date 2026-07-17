namespace Domain.Entities
{
    public class UsageLog
    {
        protected UsageLog()
        {
        }

        public UsageLog(
            int bookingItemId,
            DateTime actualCheckin)
        {
            if (bookingItemId <= 0)
            {
                throw new ArgumentException(
                    "BookingItemId phải lớn hơn 0.");
            }

            BookingItemId = bookingItemId;
            ActualCheckin = actualCheckin;
            IncidentStatus = UsageIncidentStatus.None;
            IncidentReviewStatus = IncidentReviewStatus.NotRequired;
        }

        public int LogId { get; private set; }

        public int BookingItemId { get; private set; }

        public DateTime ActualCheckin { get; private set; }

        public DateTime? ActualCheckout { get; private set; }

        public UsageIncidentStatus IncidentStatus { get; private set; }
            = UsageIncidentStatus.None;

        public string? IncidentDescription { get; private set; }

        public int? AffectedEquipmentId { get; private set; }

        public IncidentReviewStatus IncidentReviewStatus { get; private set; }
            = IncidentReviewStatus.NotRequired;

        public int? IncidentReviewedById { get; private set; }

        public DateTime? IncidentReviewedAt { get; private set; }

        public string? IncidentReviewNote { get; private set; }

        public BookingItem? BookingItem { get; private set; }

        public Equipment? AffectedEquipment { get; private set; }

        public User? IncidentReviewedBy { get; private set; }

        public void CheckOut(DateTime actualCheckout)
        {
            if (ActualCheckout.HasValue)
            {
                throw new InvalidOperationException(
                    "Lượt sử dụng này đã checkout.");
            }

            if (actualCheckout <= ActualCheckin)
            {
                throw new ArgumentException(
                    "Thời gian checkout phải lớn hơn thời gian checkin.");
            }

            ActualCheckout = actualCheckout;
        }

        public void ReportIncident(
            UsageIncidentStatus incidentStatus,
            string incidentDescription,
            int? affectedEquipmentId = null)
        {
            if (incidentStatus == UsageIncidentStatus.None)
            {
                throw new ArgumentException(
                    "Trạng thái sự cố không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(incidentDescription))
            {
                throw new ArgumentException(
                    "Mô tả sự cố không được để trống.");
            }

            if (affectedEquipmentId.HasValue
                && affectedEquipmentId.Value <= 0)
            {
                throw new ArgumentException(
                    "AffectedEquipmentId phải lớn hơn 0.");
            }

            if (IncidentReviewStatus == IncidentReviewStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Sự cố hiện đang chờ LabManager xác nhận.");
            }

            IncidentStatus = incidentStatus;
            IncidentDescription = incidentDescription.Trim();
            AffectedEquipmentId = affectedEquipmentId;
            IncidentReviewedById = null;
            IncidentReviewedAt = null;
            IncidentReviewNote = null;

            IncidentReviewStatus = incidentStatus is
                UsageIncidentStatus.DamageReported
                or UsageIncidentStatus.MissingEquipment
                    ? IncidentReviewStatus.Pending
                    : IncidentReviewStatus.NotRequired;
        }

        public void ConfirmIncident(
            int reviewedById,
            string? reviewNote = null)
        {
            ValidateReviewer(reviewedById);

            if (IncidentReviewStatus != IncidentReviewStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Chỉ sự cố đang Pending mới được xác nhận.");
            }

            if (IncidentStatus is not UsageIncidentStatus.DamageReported
                and not UsageIncidentStatus.MissingEquipment)
            {
                throw new InvalidOperationException(
                    "Sự cố này không yêu cầu xác nhận thiết bị.");
            }

            if (!AffectedEquipmentId.HasValue)
            {
                throw new InvalidOperationException(
                    "Sự cố chưa xác định thiết bị bị ảnh hưởng.");
            }

            IncidentReviewStatus = IncidentReviewStatus.Confirmed;
            IncidentReviewedById = reviewedById;
            IncidentReviewedAt = DateTime.UtcNow;
            IncidentReviewNote = NormalizeReviewNote(reviewNote);
        }

        public void RejectIncident(
            int reviewedById,
            string? reviewNote = null)
        {
            ValidateReviewer(reviewedById);

            if (IncidentReviewStatus != IncidentReviewStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Chỉ sự cố đang Pending mới được từ chối.");
            }

            IncidentReviewStatus = IncidentReviewStatus.Rejected;
            IncidentReviewedById = reviewedById;
            IncidentReviewedAt = DateTime.UtcNow;
            IncidentReviewNote = NormalizeReviewNote(reviewNote);
        }

        private static void ValidateReviewer(int reviewedById)
        {
            if (reviewedById <= 0)
            {
                throw new ArgumentException(
                    "ReviewedById phải lớn hơn 0.");
            }
        }

        private static string? NormalizeReviewNote(string? reviewNote)
        {
            if (string.IsNullOrWhiteSpace(reviewNote))
                return null;

            string value = reviewNote.Trim();
            if (value.Length > 1000)
            {
                throw new ArgumentException(
                    "Ghi chú xác nhận không được vượt quá 1000 ký tự.");
            }

            return value;
        }
    }
}
