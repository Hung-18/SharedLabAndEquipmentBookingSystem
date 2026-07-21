namespace Application.DTOs.UsageLogs
{
    public class CheckInUsageRequest
    {
        public int BookingItemId { get; set; }

        // Production clients should leave this null. The server uses DateTime.UtcNow.
        // Only Admin/LabManager may provide a historical value for data correction.
        public DateTime? ActualCheckin { get; set; }
    }
}
