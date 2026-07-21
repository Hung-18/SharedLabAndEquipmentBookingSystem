namespace Application.DTOs.Booking
{
    public class CalendarEventResponse
    {
        public string EventType { get; set; } = string.Empty;
        public int SourceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Blocking { get; set; }
        public int? UserId { get; set; }
        public List<CalendarResourceResponse> Resources { get; set; } = new();
    }

    public class CalendarResourceResponse
    {
        public string ResourceType { get; set; } = string.Empty;
        public int ResourceId { get; set; }
        public int LabId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
    }
}
