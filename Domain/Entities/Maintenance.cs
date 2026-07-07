

namespace Domain.Entities
{
    public class Maintenance
    {
        protected Maintenance() { }

        public Maintenance(
            int createdById,
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            decimal maintenanceCost = 0,
            string? notes = null)
        {
            if (createdById <= 0)
                throw new ArgumentException("CreatedById phải lớn hơn 0");

            if (labId == null && equipmentId == null)
                throw new ArgumentException("Phải chọn phòng lab hoặc thiết bị để bảo trì");

            if (startTime >= endTime)
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");

            if (maintenanceCost < 0)
                throw new ArgumentException("Chi phí bảo trì không được âm");

            CreatedById = createdById;
            LabId = labId;
            EquipmentId = equipmentId;
            StartTime = startTime;
            EndTime = endTime;
            MaintenanceCost = maintenanceCost;
            Notes = notes?.Trim();
            Status = MaintenanceStatus.Scheduled;
        }

        public int MaintenanceId { get; private set; }

        public int? LabId { get; private set; }

        public int? EquipmentId { get; private set; }

        public int CreatedById { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public decimal MaintenanceCost { get; private set; }

        public string? Notes { get; private set; }

        public MaintenanceStatus Status { get; private set; } = MaintenanceStatus.Scheduled;

        public LabRoom? LabRoom { get; private set; }

        public Equipment? Equipment { get; private set; }

        public User? CreatedBy { get; private set; }

        public void Start()
        {
            Status = MaintenanceStatus.InProgress;
        }

        public void Complete()
        {
            Status = MaintenanceStatus.Completed;
        }

        public void Cancel()
        {
            Status = MaintenanceStatus.Cancelled;
        }
    }
}