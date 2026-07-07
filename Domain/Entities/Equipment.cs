

namespace Domain.Entities
{
    public class Equipment
    {
        protected Equipment() { }

        public Equipment(
            int labId,
            string equipmentName,
            string? modelSpecs = null,
            string? imageUrl = null,
            string? usageGuideline = null)
        {
            if (labId <= 0)
                throw new ArgumentException("LabId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(equipmentName))
                throw new ArgumentException("Tên thiết bị không được để trống");

            LabId = labId;
            EquipmentName = equipmentName.Trim();
            ModelSpecs = modelSpecs?.Trim();
            ImageUrl = imageUrl?.Trim();
            UsageGuideline = usageGuideline?.Trim();
            Status = EquipmentStatus.Available;
        }

        public int EquipmentId { get; private set; }

        public int LabId { get; private set; }

        public string EquipmentName { get; private set; } = string.Empty;

        public string? ModelSpecs { get; private set; }

        public string? ImageUrl { get; private set; }

        public string? UsageGuideline { get; private set; }

        public EquipmentStatus Status { get; private set; } = EquipmentStatus.Available;

        public LabRoom? LabRoom { get; private set; }

        public ICollection<BookingItem> BookingItems { get; private set; } = new List<BookingItem>();

        public ICollection<Maintenance> Maintenances { get; private set; } = new List<Maintenance>();

        public ICollection<Waitlist> Waitlists { get; private set; } = new List<Waitlist>();
    }
}