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

        public MaintenanceRecurrenceType RecurrenceType { get; private set; }
            = MaintenanceRecurrenceType.None;

        public int RecurrenceInterval { get; private set; } = 1;

        public DateTime? RecurrenceEndDate { get; private set; }

        public int? ParentMaintenanceId { get; private set; }

        public bool NextOccurrenceCreated { get; private set; }

        public bool RecurrenceStopped { get; private set; }

        public int? PreviousResourceStatus { get; private set; }

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

            bool scheduleOrResourceChanged =
                LabId != labId
                || EquipmentId != equipmentId
                || StartTime != startTime
                || EndTime != endTime;

            if (NextOccurrenceCreated
                && scheduleOrResourceChanged)
            {
                throw new InvalidOperationException(
                    "Không thể đổi tài nguyên hoặc thời gian sau khi occurrence tiếp theo đã được sinh.");
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

        public void ConfigureRecurrence(
            MaintenanceRecurrenceType recurrenceType,
            int recurrenceInterval,
            DateTime? recurrenceEndDate,
            int? parentMaintenanceId = null)
        {
            if (!Enum.IsDefined(recurrenceType))
                throw new ArgumentException("Kiểu lặp bảo trì không hợp lệ.");

            bool recurrenceChanged =
                RecurrenceType != recurrenceType
                || RecurrenceInterval != recurrenceInterval
                || RecurrenceEndDate != recurrenceEndDate
                || ParentMaintenanceId != parentMaintenanceId;

            if (NextOccurrenceCreated && recurrenceChanged)
            {
                throw new InvalidOperationException(
                    "Không thể đổi quy tắc lặp sau khi occurrence tiếp theo đã được sinh.");
            }

            if (recurrenceType == MaintenanceRecurrenceType.None)
            {
                RecurrenceType = MaintenanceRecurrenceType.None;
                RecurrenceInterval = 1;
                RecurrenceEndDate = null;
                ParentMaintenanceId = parentMaintenanceId;

                if (recurrenceChanged)
                    NextOccurrenceCreated = false;

                return;
            }

            if (recurrenceInterval <= 0)
                throw new ArgumentException("Khoảng lặp phải lớn hơn 0.");

            if (recurrenceEndDate.HasValue
                && recurrenceEndDate.Value <= StartTime)
            {
                throw new ArgumentException(
                    "Ngày kết thúc chu kỳ phải sau thời gian bắt đầu bảo trì.");
            }

            RecurrenceType = recurrenceType;
            RecurrenceInterval = recurrenceInterval;
            RecurrenceEndDate = recurrenceEndDate;
            ParentMaintenanceId = parentMaintenanceId;
            RecurrenceStopped = false;

            if (recurrenceChanged)
                NextOccurrenceCreated = false;
        }

        public DateTime GetNextOccurrenceStart()
        {
            return RecurrenceType switch
            {
                MaintenanceRecurrenceType.Daily =>
                    StartTime.AddDays(RecurrenceInterval),
                MaintenanceRecurrenceType.Weekly =>
                    StartTime.AddDays(7 * RecurrenceInterval),
                MaintenanceRecurrenceType.Monthly =>
                    StartTime.AddMonths(RecurrenceInterval),
                _ => throw new InvalidOperationException(
                    "Lịch bảo trì này không được cấu hình lặp.")
            };
        }

        public void MarkNextOccurrenceCreated()
        {
            if (RecurrenceType == MaintenanceRecurrenceType.None)
                throw new InvalidOperationException("Lịch bảo trì không lặp.");
            NextOccurrenceCreated = true;
        }

        public void StopRecurrence()
        {
            if (RecurrenceType == MaintenanceRecurrenceType.None
                && !ParentMaintenanceId.HasValue)
            {
                return;
            }

            RecurrenceStopped = true;
        }

        public void CapturePreviousResourceStatus(int statusValue)
        {
            if (Status != MaintenanceStatus.Scheduled)
            {
                throw new InvalidOperationException(
                    "Chỉ được ghi trạng thái tài nguyên trước khi bắt đầu bảo trì.");
            }

            PreviousResourceStatus = statusValue;
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