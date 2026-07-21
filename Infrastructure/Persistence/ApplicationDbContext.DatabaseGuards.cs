using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AppDbContext
{
    public partial class ApplicationDbContext
    {
        // Call this once after Database.Migrate() in Program.cs.
        // Reason: HasTrigger only tells EF Core that a table has a trigger.
        // It does NOT create the SQL trigger in database.
        public async Task EnsureDatabaseGuardsCreatedAsync(CancellationToken cancellationToken = default)
        {
            await Database.ExecuteSqlRawAsync(CreateOpenUsageLogUniqueIndexSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateActiveViolationUniqueIndexSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateActiveWaitlistUniqueIndexesSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterBookingItemsPreventConflictTriggerSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterBookingsPreventConflictTriggerSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateOrAlterMaintenancesPreventConflictTriggerSql, cancellationToken);
        }

        // App-level guard before creating/updating a booking item.
        // It checks both booking-vs-booking and booking-vs-maintenance.
        public Task<bool> HasLabScheduleConflictAsync(
            int labId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId = null,
            CancellationToken cancellationToken = default)
        {
            if (labId <= 0)
                throw new ArgumentException("LabId must be greater than 0", nameof(labId));

            return HasResourceScheduleConflictAsync(
                labId: labId,
                equipmentId: null,
                startTime: startTime,
                endTime: endTime,
                excludedBookingId: excludedBookingId,
                cancellationToken: cancellationToken);
        }

        // App-level guard before creating/updating a booking item.
        // It checks both booking-vs-booking and booking-vs-maintenance.
        public Task<bool> HasEquipmentScheduleConflictAsync(
            int equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId = null,
            CancellationToken cancellationToken = default)
        {
            if (equipmentId <= 0)
                throw new ArgumentException("EquipmentId must be greater than 0", nameof(equipmentId));

            return HasResourceScheduleConflictAsync(
                labId: null,
                equipmentId: equipmentId,
                startTime: startTime,
                endTime: endTime,
                excludedBookingId: excludedBookingId,
                cancellationToken: cancellationToken);
        }

        private async Task<bool> HasResourceScheduleConflictAsync(
            int? labId,
            int? equipmentId,
            DateTime startTime,
            DateTime endTime,
            int? excludedBookingId,
            CancellationToken cancellationToken)
        {
            if (startTime >= endTime)
                throw new ArgumentException("Start time must be earlier than end time");

            if ((labId.HasValue && equipmentId.HasValue) || (!labId.HasValue && !equipmentId.HasValue))
                throw new ArgumentException("Exactly one resource must be selected");

            var activeBookingStatuses = new[]
            {
                BookingStatus.Approved
            };

            var activeMaintenanceStatuses = new[]
            {
                MaintenanceStatus.Scheduled,
                MaintenanceStatus.InProgress
            };

            int? equipmentLabId = null;
            if (equipmentId.HasValue)
            {
                equipmentLabId = await Equipments
                    .Where(x => x.EquipmentId == equipmentId.Value)
                    .Select(x => (int?)x.LabId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var bookingConflict = await BookingItems.AnyAsync(x =>
                x.Booking != null
                && activeBookingStatuses.Contains(x.Booking.Status)
                && x.Booking.StartTime < endTime
                && x.Booking.EndTime > startTime
                && (!excludedBookingId.HasValue || x.BookingId != excludedBookingId.Value)
                && (
                    (labId.HasValue
                        && (x.LabId == labId.Value
                            || (x.Equipment != null && x.Equipment.LabId == labId.Value)))
                    ||
                    (equipmentId.HasValue
                        && (x.EquipmentId == equipmentId.Value
                            || (equipmentLabId.HasValue && x.LabId == equipmentLabId.Value)))
                ),
                cancellationToken);

            if (bookingConflict)
                return true;

            var maintenanceConflict = await Maintenances.AnyAsync(x =>
                activeMaintenanceStatuses.Contains(x.Status) &&
                x.StartTime < endTime &&
                x.EndTime > startTime &&
                (
                    (labId.HasValue && x.LabId == labId.Value) ||
                    (equipmentId.HasValue &&
                        (x.EquipmentId == equipmentId.Value ||
                         (equipmentLabId.HasValue && x.LabId == equipmentLabId.Value)))
                ),
                cancellationToken);

            return maintenanceConflict;
        }

        private const string CreateOpenUsageLogUniqueIndexSql = """
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'UX_UsageLogs_OneOpenPerBookingItem'
      AND [object_id] = OBJECT_ID(N'[UsageLogs]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_UsageLogs_OneOpenPerBookingItem]
        ON [UsageLogs] ([BookingItemId])
        WHERE [ActualCheckout] IS NULL;
END
""";

        private const string CreateActiveViolationUniqueIndexSql = """
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = 'UX_Violations_OneActivePerBookingType'
      AND [object_id] = OBJECT_ID(N'[Violations]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Violations_OneActivePerBookingType]
        ON [Violations] ([UserId], [BookingId], [ViolationType])
        WHERE [Status] = 'Active';
END
""";

        private const string CreateActiveWaitlistUniqueIndexesSql = """
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = 'UX_Waitlists_ActiveUserResourceSlot'
      AND [object_id] = OBJECT_ID(N'[Waitlists]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Waitlists_ActiveUserResourceSlot]
        ON [Waitlists]
        ([UserId], [LabId], [EquipmentId], [RequestedStart], [RequestedEnd])
        WHERE [Status] IN ('Waiting', 'Notified');
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = 'UX_Waitlists_ActiveQueuePosition'
      AND [object_id] = OBJECT_ID(N'[Waitlists]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Waitlists_ActiveQueuePosition]
        ON [Waitlists]
        ([LabId], [EquipmentId], [RequestedStart], [RequestedEnd], [QueuePosition])
        WHERE [Status] IN ('Waiting', 'Notified');
END;
""";

        private const string CreateOrAlterBookingItemsPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_BookingItems_PreventConflict]
ON [BookingItems]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Pending requests may share a slot, but an existing Approved booking locks the resource.
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN [Bookings] b
            ON b.[BookingId] = i.[BookingId]
        LEFT JOIN [Equipments] insertedEquipment
            ON insertedEquipment.[EquipmentId] = i.[EquipmentId]
        INNER JOIN [BookingItems] otherItem
            ON otherItem.[BookingItemId] <> i.[BookingItemId]
        LEFT JOIN [Equipments] otherEquipment
            ON otherEquipment.[EquipmentId] = otherItem.[EquipmentId]
        INNER JOIN [Bookings] otherBooking
            ON otherBooking.[BookingId] = otherItem.[BookingId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND otherBooking.[Status] = 'Approved'
          AND b.[BookingId] <> otherBooking.[BookingId]
          AND b.[StartTime] < otherBooking.[EndTime]
          AND b.[EndTime] > otherBooking.[StartTime]
          AND
          (
              (i.[LabId] IS NOT NULL AND otherItem.[LabId] = i.[LabId])
              OR
              (i.[EquipmentId] IS NOT NULL
               AND otherItem.[EquipmentId] = i.[EquipmentId])
              OR
              (i.[LabId] IS NOT NULL
               AND otherEquipment.[LabId] = i.[LabId])
              OR
              (insertedEquipment.[LabId] IS NOT NULL
               AND otherItem.[LabId] = insertedEquipment.[LabId])
          )
    )
    BEGIN
        THROW 50001, 'Booking schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN [Bookings] b
            ON b.[BookingId] = i.[BookingId]
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = i.[EquipmentId]
        INNER JOIN [Maintenances] m ON 1 = 1
        LEFT JOIN [Equipments] maintenanceEquipment
            ON maintenanceEquipment.[EquipmentId] = m.[EquipmentId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND
          (
              (i.[LabId] IS NOT NULL AND m.[LabId] = i.[LabId])
              OR
              (i.[EquipmentId] IS NOT NULL
               AND m.[EquipmentId] = i.[EquipmentId])
              OR
              (i.[EquipmentId] IS NOT NULL
               AND m.[LabId] = itemEquipment.[LabId])
              OR
              (i.[LabId] IS NOT NULL
               AND maintenanceEquipment.[LabId] = i.[LabId])
          )
          AND m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[StartTime] < m.[EndTime]
          AND b.[EndTime] > m.[StartTime]
    )
    BEGIN
        THROW 50002, 'Booking schedule overlaps with maintenance time.', 1;
    END;
END
""";

        private const string CreateOrAlterBookingsPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_Bookings_PreventConflict]
ON [Bookings]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Approval is the point at which a booking locks a resource.
    IF EXISTS
    (
        SELECT 1
        FROM inserted b
        INNER JOIN [BookingItems] item
            ON item.[BookingId] = b.[BookingId]
        LEFT JOIN [Equipments] itemEquipmentForConflict
            ON itemEquipmentForConflict.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [BookingItems] otherItem
            ON otherItem.[BookingItemId] <> item.[BookingItemId]
        LEFT JOIN [Equipments] otherEquipmentForConflict
            ON otherEquipmentForConflict.[EquipmentId] = otherItem.[EquipmentId]
        INNER JOIN [Bookings] otherBooking
            ON otherBooking.[BookingId] = otherItem.[BookingId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND otherBooking.[Status] = 'Approved'
          AND otherBooking.[BookingId] <> b.[BookingId]
          AND b.[StartTime] < otherBooking.[EndTime]
          AND b.[EndTime] > otherBooking.[StartTime]
          AND
          (
              (item.[LabId] IS NOT NULL
               AND otherItem.[LabId] = item.[LabId])
              OR
              (item.[EquipmentId] IS NOT NULL
               AND otherItem.[EquipmentId] = item.[EquipmentId])
              OR
              (item.[LabId] IS NOT NULL
               AND otherEquipmentForConflict.[LabId] = item.[LabId])
              OR
              (itemEquipmentForConflict.[LabId] IS NOT NULL
               AND otherItem.[LabId] = itemEquipmentForConflict.[LabId])
          )
    )
    BEGIN
        THROW 50003, 'Booking schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted b
        INNER JOIN [BookingItems] item
            ON item.[BookingId] = b.[BookingId]
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [Maintenances] m ON 1 = 1
        LEFT JOIN [Equipments] maintenanceEquipment
            ON maintenanceEquipment.[EquipmentId] = m.[EquipmentId]
        WHERE b.[Status] IN ('Pending', 'Approved')
          AND
          (
              (item.[LabId] IS NOT NULL AND m.[LabId] = item.[LabId])
              OR
              (item.[EquipmentId] IS NOT NULL
               AND m.[EquipmentId] = item.[EquipmentId])
              OR
              (item.[EquipmentId] IS NOT NULL
               AND m.[LabId] = itemEquipment.[LabId])
              OR
              (item.[LabId] IS NOT NULL
               AND maintenanceEquipment.[LabId] = item.[LabId])
          )
          AND m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[StartTime] < m.[EndTime]
          AND b.[EndTime] > m.[StartTime]
    )
    BEGIN
        THROW 50004, 'Booking schedule overlaps with maintenance time.', 1;
    END;
END
""";

        private const string CreateOrAlterMaintenancesPreventConflictTriggerSql = """
CREATE OR ALTER TRIGGER [TRG_Maintenances_PreventConflict]
ON [Maintenances]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted m
        LEFT JOIN [Equipments] insertedEquipmentForBooking
            ON insertedEquipmentForBooking.[EquipmentId] = m.[EquipmentId]
        INNER JOIN [BookingItems] item ON 1 = 1
        LEFT JOIN [Equipments] itemEquipment
            ON itemEquipment.[EquipmentId] = item.[EquipmentId]
        INNER JOIN [Bookings] b
            ON b.[BookingId] = item.[BookingId]
        WHERE m.[Status] IN ('Scheduled', 'InProgress')
          AND b.[Status] = 'Approved'
          AND m.[StartTime] < b.[EndTime]
          AND m.[EndTime] > b.[StartTime]
          AND
          (
              (m.[LabId] IS NOT NULL
               AND (item.[LabId] = m.[LabId]
                    OR itemEquipment.[LabId] = m.[LabId]))
              OR
              (m.[EquipmentId] IS NOT NULL
               AND item.[EquipmentId] = m.[EquipmentId])
              OR
              (m.[EquipmentId] IS NOT NULL
               AND item.[LabId] = insertedEquipmentForBooking.[LabId])
          )
    )
    BEGIN
        THROW 50005, 'Maintenance schedule overlaps with an existing approved booking.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted m
        LEFT JOIN [Equipments] insertedEquipment
            ON insertedEquipment.[EquipmentId] = m.[EquipmentId]
        INNER JOIN [Maintenances] otherMaintenance
            ON otherMaintenance.[MaintenanceId] <> m.[MaintenanceId]
        LEFT JOIN [Equipments] otherEquipment
            ON otherEquipment.[EquipmentId] = otherMaintenance.[EquipmentId]
        WHERE m.[Status] IN ('Scheduled', 'InProgress')
          AND otherMaintenance.[Status] IN ('Scheduled', 'InProgress')
          AND m.[StartTime] < otherMaintenance.[EndTime]
          AND m.[EndTime] > otherMaintenance.[StartTime]
          AND
          (
              (m.[LabId] IS NOT NULL
               AND otherMaintenance.[LabId] = m.[LabId])
              OR
              (m.[EquipmentId] IS NOT NULL
               AND otherMaintenance.[EquipmentId] = m.[EquipmentId])
              OR
              (m.[LabId] IS NOT NULL
               AND otherEquipment.[LabId] = m.[LabId])
              OR
              (m.[EquipmentId] IS NOT NULL
               AND otherMaintenance.[LabId] = insertedEquipment.[LabId])
          )
    )
    BEGIN
        THROW 50006, 'Maintenance schedule overlaps with an existing maintenance schedule.', 1;
    END;
END
""";

    }
}
