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
            ValidateDetails(labId, equipmentName);
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

        public void UpdateDetails(
            int labId,
            string equipmentName,
            string? modelSpecs = null,
            string? imageUrl = null,
            string? usageGuideline = null)
        {
            ValidateDetails(labId, equipmentName);
            LabId = labId;
            EquipmentName = equipmentName.Trim();
            ModelSpecs = modelSpecs?.Trim();
            ImageUrl = imageUrl?.Trim();
            UsageGuideline = usageGuideline?.Trim();
        }

        public void MarkInUse()
        {
            if (Status != EquipmentStatus.Available)
                throw new InvalidOperationException(
                    "Chỉ thiết bị Available mới được chuyển sang InUse.");
            Status = EquipmentStatus.InUse;
        }

        public void MarkAvailable()
        {
            if (Status is EquipmentStatus.Retired or EquipmentStatus.Broken)
                return;
            Status = EquipmentStatus.Available;
        }

        public void StartMaintenance(bool allowBroken = false)
        {
            if (Status == EquipmentStatus.Retired)
                throw new InvalidOperationException(
                    "Không thể bảo trì thiết bị đã ngừng sử dụng.");

            if (Status == EquipmentStatus.InUse)
                throw new InvalidOperationException(
                    "Không thể bắt đầu bảo trì khi thiết bị đang được sử dụng.");

            if (Status == EquipmentStatus.Maintenance)
                throw new InvalidOperationException(
                    "Thiết bị đang trong trạng thái bảo trì.");

            if (Status == EquipmentStatus.Broken && !allowBroken)
                throw new InvalidOperationException(
                    "Thiết bị Broken chỉ được bắt đầu bằng lịch bảo trì trực tiếp cho thiết bị.");

            Status = EquipmentStatus.Maintenance;
        }

        public void FinishMaintenance()
        {
            if (Status != EquipmentStatus.Maintenance)
                return;

            Status = EquipmentStatus.Available;
        }

        public void RestoreAfterCancelledMaintenance(
            EquipmentStatus previousStatus)
        {
            if (Status != EquipmentStatus.Maintenance)
                return;

            if (previousStatus is not EquipmentStatus.Available
                and not EquipmentStatus.Broken)
            {
                throw new InvalidOperationException(
                    "Trạng thái trước bảo trì của thiết bị không hợp lệ.");
            }

            Status = previousStatus;
        }

        public void MarkBroken()
        {
            if (Status == EquipmentStatus.Retired)
                throw new InvalidOperationException("Thiết bị đã ngừng sử dụng.");
            Status = EquipmentStatus.Broken;
        }

        public void Retire() => Status = EquipmentStatus.Retired;

        private static void ValidateDetails(int labId, string equipmentName)
        {
            if (labId <= 0)
                throw new ArgumentException("LabId phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(equipmentName))
                throw new ArgumentException("Tên thiết bị không được để trống.");
        }
    }
}
