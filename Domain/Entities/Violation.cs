

namespace Domain.Entities
{
    public class Violation
    {
        protected Violation() { }

        public Violation(
            int userId,
            int bookingId,
            ViolationType violationType,
            int penaltyPointsAdded)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (bookingId <= 0)
                throw new ArgumentException("BookingId phải lớn hơn 0");

            if (penaltyPointsAdded <= 0)
                throw new ArgumentException("Điểm phạt phải lớn hơn 0");

            UserId = userId;
            BookingId = bookingId;
            ViolationType = violationType;
            PenaltyPointsAdded = penaltyPointsAdded;
            LoggedAt = DateTime.UtcNow;
            Status = ViolationStatus.Active;
        }

        public int ViolationId { get; private set; }

        public int UserId { get; private set; }

        public int BookingId { get; private set; }

        public ViolationType ViolationType { get; private set; }

        public int PenaltyPointsAdded { get; private set; }

        public DateTime LoggedAt { get; private set; }

        public ViolationStatus Status { get; private set; } = ViolationStatus.Active;

        public User? User { get; private set; }

        public Booking? Booking { get; private set; }

        public void Resolve()
        {
            Status = ViolationStatus.Resolved;
        }

        public void Cancel()
        {
            Status = ViolationStatus.Cancelled;
        }
    }
}