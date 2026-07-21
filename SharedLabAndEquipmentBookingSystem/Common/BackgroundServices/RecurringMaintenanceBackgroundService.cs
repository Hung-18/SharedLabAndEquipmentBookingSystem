using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Common.BackgroundServices
{
    public sealed class RecurringMaintenanceBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);
        private static readonly TimeSpan CreationHorizon = TimeSpan.FromDays(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringMaintenanceBackgroundService> _logger;

        public RecurringMaintenanceBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecurringMaintenanceBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CreateNextOccurrencesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Không thể sinh lịch bảo trì định kỳ.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CreateNextOccurrencesAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<int> sourceIds;

            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork =
                    scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var sources = await unitOfWork.Maintenances.FindAsync(
                    x => x.RecurrenceType != MaintenanceRecurrenceType.None
                        && !x.NextOccurrenceCreated
                        && !x.RecurrenceStopped,
                    cancellationToken);

                sourceIds = sources
                    .Select(x => x.MaintenanceId)
                    .Distinct()
                    .ToList();
            }

            foreach (int sourceId in sourceIds)
            {
                try
                {
                    await ProcessSourceAsync(
                        sourceId,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    // Unique index ParentMaintenanceId + StartTime bảo vệ
                    // trường hợp nhiều instance cùng sinh một occurrence.
                    _logger.LogWarning(
                        ex,
                        "Bỏ qua occurrence bị trùng của maintenance nguồn {MaintenanceId}.",
                        sourceId);
                }
                catch (Exception ex)
                {
                    // Mỗi nguồn dùng transaction riêng, một lỗi không rollback
                    // các occurrence đã tạo thành công cho nguồn khác.
                    _logger.LogError(
                        ex,
                        "Không thể sinh occurrence cho maintenance nguồn {MaintenanceId}.",
                        sourceId);
                }
            }
        }

        private async Task ProcessSourceAsync(
            int sourceId,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork =
                scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var source =
                        await unitOfWork.Maintenances.GetDetailAsync(
                            sourceId,
                            ct);

                    if (source is null
                        || source.RecurrenceType
                            == MaintenanceRecurrenceType.None
                        || source.NextOccurrenceCreated
                        || source.RecurrenceStopped)
                    {
                        return;
                    }

                    TimeSpan duration =
                        source.EndTime - source.StartTime;
                    DateTime now = DateTime.UtcNow;
                    DateTime nextStart =
                        source.GetNextOccurrenceStart();
                    int skippedPastOccurrences = 0;

                    // Nếu kỳ tiếp theo đã bắt đầu hoặc đã kết thúc trong quá khứ,
                    // không tạo một maintenance quá hạn. Tiến tới kỳ tương lai
                    // gần nhất để chuỗi định kỳ không bị kẹt mãi ở một slot cũ.
                    while (nextStart <= now)
                    {
                        if (source.RecurrenceEndDate.HasValue
                            && nextStart
                                > source.RecurrenceEndDate.Value)
                        {
                            source.MarkNextOccurrenceCreated();
                            unitOfWork.Maintenances.Update(source);
                            await unitOfWork.SaveChangesAsync(ct);
                            return;
                        }

                        nextStart = AdvanceOccurrenceStart(
                            source,
                            nextStart);
                        skippedPastOccurrences++;

                        // Chặn vòng lặp bất thường nếu dữ liệu recurrence bị lỗi.
                        if (skippedPastOccurrences > 10000)
                        {
                            throw new InvalidOperationException(
                                $"Không thể xác định kỳ bảo trì tương lai cho maintenance {source.MaintenanceId}.");
                        }
                    }

                    if (skippedPastOccurrences > 0)
                    {
                        _logger.LogInformation(
                            "Đã bỏ qua {SkippedCount} occurrence quá hạn của maintenance {MaintenanceId}; "
                            + "kỳ tương lai gần nhất bắt đầu lúc {NextStart}.",
                            skippedPastOccurrences,
                            source.MaintenanceId,
                            nextStart);
                    }

                    if (nextStart
                        > now.Add(CreationHorizon))
                    {
                        return;
                    }

                    if (source.RecurrenceEndDate.HasValue
                        && nextStart
                            > source.RecurrenceEndDate.Value)
                    {
                        source.MarkNextOccurrenceCreated();
                        unitOfWork.Maintenances.Update(source);
                        await unitOfWork.SaveChangesAsync(ct);
                        return;
                    }

                    if (await unitOfWork.Maintenances
                        .ExistsOccurrenceAsync(
                            source.MaintenanceId,
                            nextStart,
                            ct))
                    {
                        source.MarkNextOccurrenceCreated();
                        unitOfWork.Maintenances.Update(source);
                        await unitOfWork.SaveChangesAsync(ct);
                        return;
                    }

                    DateTime nextEnd =
                        nextStart.Add(duration);

                    bool maintenanceConflict =
                        await unitOfWork.Maintenances
                            .HasMaintenanceConflictAsync(
                                source.LabId,
                                source.EquipmentId,
                                nextStart,
                                nextEnd,
                                excludeMaintenanceId: null,
                                cancellationToken: ct);

                    bool bookingConflict =
                        await unitOfWork.Maintenances
                            .HasBookingConflictForMaintenanceAsync(
                                source.LabId,
                                source.EquipmentId,
                                nextStart,
                                nextEnd,
                                excludeBookingId: null,
                                includePending: false,
                                cancellationToken: ct);

                    if (maintenanceConflict || bookingConflict)
                    {
                        _logger.LogWarning(
                            "Chưa sinh occurrence cho maintenance {MaintenanceId} "
                            + "vì khung giờ {Start} - {End} đang bị khóa.",
                            source.MaintenanceId,
                            nextStart,
                            nextEnd);
                        return;
                    }

                    var next = new Maintenance(
                        source.CreatedById,
                        source.LabId,
                        source.EquipmentId,
                        nextStart,
                        nextEnd,
                        source.MaintenanceCost,
                        source.Notes);

                    next.ConfigureRecurrence(
                        source.RecurrenceType,
                        source.RecurrenceInterval,
                        source.RecurrenceEndDate,
                        source.MaintenanceId);

                    await unitOfWork.Maintenances.AddAsync(
                        next,
                        ct);

                    source.MarkNextOccurrenceCreated();
                    unitOfWork.Maintenances.Update(source);

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }

        private static DateTime AdvanceOccurrenceStart(
            Maintenance source,
            DateTime currentStart)
        {
            return source.RecurrenceType switch
            {
                MaintenanceRecurrenceType.Daily =>
                    currentStart.AddDays(source.RecurrenceInterval),
                MaintenanceRecurrenceType.Weekly =>
                    currentStart.AddDays(7 * source.RecurrenceInterval),
                MaintenanceRecurrenceType.Monthly =>
                    currentStart.AddMonths(source.RecurrenceInterval),
                _ => throw new InvalidOperationException(
                    "Lịch bảo trì không có quy tắc lặp hợp lệ.")
            };
        }
    }
}
