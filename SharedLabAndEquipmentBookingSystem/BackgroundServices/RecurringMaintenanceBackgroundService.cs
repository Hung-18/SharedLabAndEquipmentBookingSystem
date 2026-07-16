using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace API.BackgroundServices
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CreateNextOccurrencesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể sinh lịch bảo trì định kỳ.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CreateNextOccurrencesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            DateTime horizon = DateTime.UtcNow.Add(CreationHorizon);

            var sources = await unitOfWork.Maintenances.FindAsync(
                x => x.RecurrenceType != MaintenanceRecurrenceType.None
                    && !x.NextOccurrenceCreated
                    && x.Status != MaintenanceStatus.Cancelled,
                cancellationToken);

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                foreach (var source in sources)
                {
                    DateTime nextStart = source.GetNextOccurrenceStart();
                    if (nextStart > horizon)
                        continue;
                    if (source.RecurrenceEndDate.HasValue
                        && nextStart > source.RecurrenceEndDate.Value)
                    {
                        source.MarkNextOccurrenceCreated();
                        unitOfWork.Maintenances.Update(source);
                        continue;
                    }

                    TimeSpan duration = source.EndTime - source.StartTime;
                    bool conflict = await unitOfWork.Maintenances.HasMaintenanceConflictAsync(
                        source.LabId,
                        source.EquipmentId,
                        nextStart,
                        nextStart.Add(duration),
                        null,
                        ct);
                    if (conflict)
                        continue;

                    var next = new Maintenance(
                        source.CreatedById,
                        source.LabId,
                        source.EquipmentId,
                        nextStart,
                        nextStart.Add(duration),
                        source.MaintenanceCost,
                        source.Notes);
                    next.ConfigureRecurrence(
                        source.RecurrenceType,
                        source.RecurrenceInterval,
                        source.RecurrenceEndDate,
                        source.MaintenanceId);

                    await unitOfWork.Maintenances.AddAsync(next, ct);
                    source.MarkNextOccurrenceCreated();
                    unitOfWork.Maintenances.Update(source);
                }

                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
    }
}
