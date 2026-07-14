namespace Domain.Entities
{
    public class BookingItem
    {
        protected BookingItem() { }

        public BookingItem(
            ResourceType resourceType,
            int? labId,
            int? equipmentId,
            string? note = null)
        {
            ValidateResource(resourceType, labId, equipmentId);

            ResourceType = resourceType;
            LabId = labId;
            EquipmentId = equipmentId;
            Note = note?.Trim();
        }

        public BookingItem(
            int bookingId,
            ResourceType resourceType,
            int? labId,
            int? equipmentId,
            string? note = null)
            : this(resourceType, labId, equipmentId, note)
        {
            if (bookingId <= 0)
                throw new ArgumentException("BookingId phải lớn hơn 0");

            BookingId = bookingId;
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

        private static void ValidateResource(
            ResourceType resourceType,
            int? labId,
            int? equipmentId)
        {
            if (!Enum.IsDefined(resourceType))
                throw new ArgumentException("Loại tài nguyên không hợp lệ.");

            if (resourceType == ResourceType.LabRoom)
            {
                if (!labId.HasValue || labId.Value <= 0)
                    throw new ArgumentException("Đặt phòng lab thì LabId phải lớn hơn 0.");

                if (equipmentId.HasValue)
                    throw new ArgumentException("BookingItem phòng lab không được có EquipmentId.");
            }
            else if (resourceType == ResourceType.Equipment)
            {
                if (!equipmentId.HasValue || equipmentId.Value <= 0)
                    throw new ArgumentException("Đặt thiết bị thì EquipmentId phải lớn hơn 0.");

                if (labId.HasValue)
                    throw new ArgumentException("BookingItem thiết bị không được có LabId.");
            }
        }
    }
}
