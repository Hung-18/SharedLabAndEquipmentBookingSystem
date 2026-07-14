

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
        }

        public int LogId { get; private set; }

        public int BookingItemId { get; private set; }

        public DateTime ActualCheckin { get; private set; }

        public DateTime? ActualCheckout { get; private set; }

        public UsageIncidentStatus IncidentStatus { get; private set; }
            = UsageIncidentStatus.None;

        public string? IncidentDescription { get; private set; }

        public BookingItem? BookingItem { get; private set; }

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
            string incidentDescription)
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

            IncidentStatus = incidentStatus;
            IncidentDescription = incidentDescription.Trim();
        }
    }

}