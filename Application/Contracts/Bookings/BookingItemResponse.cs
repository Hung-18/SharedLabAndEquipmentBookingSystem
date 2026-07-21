namespace Application.DTOs.Booking
{
    public class BookingItemResponse
    {
        public int BookingItemId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public int? LabId { get; set; }
        public string? LabName { get; set; }
        public int? EquipmentId { get; set; }
        public string? EquipmentName { get; set; }
        public string? Note { get; set; }
    }
}