

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
            string? imageUrl = null,
            string? usageGuideline = null)
        {
            if (managerId <= 0)
                throw new ArgumentException("ManagerId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(labName))
                throw new ArgumentException("Tên phòng lab không được để trống");

            if (string.IsNullOrWhiteSpace(roomCode))
                throw new ArgumentException("Mã phòng không được để trống");

            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Vị trí không được để trống");

            if (capacity <= 0)
                throw new ArgumentException("Sức chứa phải lớn hơn 0");

            ManagerId = managerId;
            LabName = labName.Trim();
            RoomCode = roomCode.Trim();
            Location = location.Trim();
            Capacity = capacity;
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

        public string? ImageUrl { get; private set; }

        public string? UsageGuideline { get; private set; }

        public LabRoomStatus Status { get; private set; } = LabRoomStatus.Available;

        public User? Manager { get; private set; }

        public ICollection<Equipment> Equipments { get; private set; } = new List<Equipment>();

        public ICollection<BookingItem> BookingItems { get; private set; } = new List<BookingItem>();

        public ICollection<Maintenance> Maintenances { get; private set; } = new List<Maintenance>();

        public ICollection<Waitlist> Waitlists { get; private set; } = new List<Waitlist>();
    }
}