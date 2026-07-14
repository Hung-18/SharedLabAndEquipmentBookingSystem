namespace Domain.Entities
{
    public class Maintenance
    {
        protected Maintenance()
        {
        }

        public Maintenance(
            int createdById,
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            decimal maintenanceCost = 0,
            string? notes = null)
        {
            Validate(
                createdById,
                labId,
                equipmentId,
                startTime,
                endTime,
                maintenanceCost);

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

        public MaintenanceStatus Status { get; private set; }
            = MaintenanceStatus.Scheduled;

        public LabRoom? LabRoom { get; private set; }

        public Equipment? Equipment { get; private set; }

        public User? CreatedBy { get; private set; }

        public void UpdateDetails(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            decimal maintenanceCost,
            string? notes)
        {
            if (Status != MaintenanceStatus.Scheduled)
            {
                throw new InvalidOperationException(
                    "Chỉ được sửa lịch bảo trì đang ở trạng thái Scheduled.");
            }

            Validate(
                CreatedById,
                labId,
                equipmentId,
                startTime,
                endTime,
                maintenanceCost);

            LabId = labId;
            EquipmentId = equipmentId;
            StartTime = startTime;
            EndTime = endTime;
            MaintenanceCost = maintenanceCost;
            Notes = notes?.Trim();
        }

        public void Start()
        {
            if (Status != MaintenanceStatus.Scheduled)
            {
                throw new InvalidOperationException(
                    "Chỉ lịch bảo trì Scheduled mới được bắt đầu.");
            }

            Status = MaintenanceStatus.InProgress;
        }

        public void Complete()
        {
            if (Status != MaintenanceStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "Chỉ lịch bảo trì InProgress mới được hoàn thành.");
            }

            Status = MaintenanceStatus.Completed;
        }

        public void Cancel()
        {
            if (Status == MaintenanceStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Không thể hủy lịch bảo trì đã hoàn thành.");
            }

            if (Status == MaintenanceStatus.Cancelled)
            {
                return;
            }

            Status = MaintenanceStatus.Cancelled;
        }

        private static void Validate(
            int createdById,
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            decimal maintenanceCost)
        {
            if (createdById <= 0)
            {
                throw new ArgumentException(
                    "CreatedById phải lớn hơn 0.");
            }

            // Bắt buộc chọn đúng một tài nguyên
            if (labId.HasValue == equipmentId.HasValue)
            {
                throw new ArgumentException(
                    "Phải chọn đúng một trong hai: phòng lab hoặc thiết bị.");
            }

            if (labId.HasValue && labId.Value <= 0)
            {
                throw new ArgumentException(
                    "LabId phải lớn hơn 0.");
            }

            if (equipmentId.HasValue && equipmentId.Value <= 0)
            {
                throw new ArgumentException(
                    "EquipmentId phải lớn hơn 0.");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException(
                    "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            if (maintenanceCost < 0)
            {
                throw new ArgumentException(
                    "Chi phí bảo trì không được âm.");
            }
        }
    }
}