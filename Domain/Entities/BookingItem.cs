

namespace Domain.Entities
{
    public class BookingItem
    {
        protected BookingItem() { }

        public BookingItem(
            int bookingId,
            ResourceType resourceType,
            int? labId,
            int? equipmentId,
            string? note = null)
        {
            if (bookingId <= 0)
                throw new ArgumentException("BookingId phải lớn hơn 0");

            if (resourceType == ResourceType.LabRoom && labId == null)
                throw new ArgumentException("Đặt phòng lab thì LabId không được null");

            if (resourceType == ResourceType.Equipment && equipmentId == null)
                throw new ArgumentException("Đặt thiết bị thì EquipmentId không được null");

            BookingId = bookingId;
            ResourceType = resourceType;
            LabId = labId;
            EquipmentId = equipmentId;
            Note = note?.Trim();
        }

        public int BookingItemId { get; private set; }

        public int BookingId { get; private set; }

        public int? LabId { get; private set; }

        public int? EquipmentId { get; private set; }

        public ResourceType ResourceType { get; private set; }

        public string? Note { get; private set; }

        public Booking? Booking { get; private set; }

        public LabRoom? LabRoom { get; private set; }

        public Equipment? Equipment { get; private set; }

        public ICollection<UsageLog> UsageLogs { get; private set; } = new List<UsageLog>();
    }
}