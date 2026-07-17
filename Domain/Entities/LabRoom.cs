namespace Domain.Entities
{
    public class LabRoom
    {
        protected LabRoom() { }

        public LabRoom(
            int managerId,
            string labName,
            string roomCode,
            string location,
            int capacity,
            string? description = null,
            string? imageUrl = null,
            string? usageGuideline = null)
        {
            ValidateManagerId(managerId);
            ValidateDetails(labName, roomCode, location, capacity);

            ManagerId = managerId;
            LabName = labName.Trim();
            RoomCode = roomCode.Trim();
            Location = location.Trim();
            Capacity = capacity;
            Description = description?.Trim();
            ImageUrl = imageUrl?.Trim();
            UsageGuideline = usageGuideline?.Trim();
            Status = LabRoomStatus.Available;
        }

        public int LabId { get; private set; }
        public int ManagerId { get; private set; }
        public string LabName { get; private set; } = string.Empty;
        public string RoomCode { get; private set; } = string.Empty;
        public string Location { get; private set; } = string.Empty;
        public int Capacity { get; private set; }
        public string? Description { get; private set; }
        public string? ImageUrl { get; private set; }
        public string? UsageGuideline { get; private set; }
        public LabRoomStatus Status { get; private set; } = LabRoomStatus.Available;
        public User? Manager { get; private set; }
        public ICollection<Equipment> Equipments { get; private set; } = new List<Equipment>();
        public ICollection<BookingItem> BookingItems { get; private set; } = new List<BookingItem>();
        public ICollection<Maintenance> Maintenances { get; private set; } = new List<Maintenance>();
        public ICollection<Waitlist> Waitlists { get; private set; } = new List<Waitlist>();

        public void UpdateDetails(
            string labName,
            string location,
            int capacity,
            string? description = null,
            string? imageUrl = null,
            string? usageGuideline = null)
        {
            ValidateDetails(labName, RoomCode, location, capacity);
            LabName = labName.Trim();
            Location = location.Trim();
            Capacity = capacity;
            Description = description?.Trim();
            ImageUrl = imageUrl?.Trim();
            UsageGuideline = usageGuideline?.Trim();
        }

        public void ChangeManager(int managerId)
        {
            ValidateManagerId(managerId);
            ManagerId = managerId;
        }

        public void StartMaintenance()
        {
            if (Status == LabRoomStatus.Inactive)
                throw new InvalidOperationException(
                    "Không thể bảo trì phòng lab đã ngừng hoạt động.");

            if (Status == LabRoomStatus.Maintenance)
                throw new InvalidOperationException(
                    "Phòng lab đang trong trạng thái bảo trì.");

            Status = LabRoomStatus.Maintenance;
        }

        public void FinishMaintenance()
        {
            if (Status == LabRoomStatus.Maintenance)
                Status = LabRoomStatus.Available;
        }

        public void RestoreAfterCancelledMaintenance(
            LabRoomStatus previousStatus)
        {
            if (Status != LabRoomStatus.Maintenance)
                return;

            if (previousStatus is not LabRoomStatus.Available
                and not LabRoomStatus.Unavailable)
            {
                throw new InvalidOperationException(
                    "Trạng thái trước bảo trì của phòng lab không hợp lệ.");
            }

            Status = previousStatus;
        }

        public void MarkUnavailable()
        {
            if (Status == LabRoomStatus.Inactive)
                throw new InvalidOperationException("Phòng lab đã ngừng hoạt động.");
            Status = LabRoomStatus.Unavailable;
        }

        public void MarkAvailable()
        {
            if (Status == LabRoomStatus.Inactive)
                throw new InvalidOperationException("Phòng lab đã ngừng hoạt động.");
            Status = LabRoomStatus.Available;
        }

        public void Deactivate() => Status = LabRoomStatus.Inactive;

        private static void ValidateManagerId(int managerId)
        {
            if (managerId <= 0)
                throw new ArgumentException("ManagerId phải lớn hơn 0.");
        }

        private static void ValidateDetails(
            string labName,
            string roomCode,
            string location,
            int capacity)
        {
            if (string.IsNullOrWhiteSpace(labName))
                throw new ArgumentException("Tên phòng lab không được để trống.");
            if (string.IsNullOrWhiteSpace(roomCode))
                throw new ArgumentException("Mã phòng không được để trống.");
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Vị trí không được để trống.");
            if (capacity <= 0)
                throw new ArgumentException("Sức chứa phải lớn hơn 0.");
        }
    }
}
