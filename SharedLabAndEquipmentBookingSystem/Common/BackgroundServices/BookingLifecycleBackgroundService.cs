using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace API.Common.BackgroundServices
{
    public sealed class BookingLifecycleBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingLifecycleBackgroundService> _logger;

        public BookingLifecycleBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingLifecycleBackgroundService> logger)
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
                    await RejectExpiredPendingBookingsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể xử lý booking Pending quá hạn.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task RejectExpiredPendingBookingsAsync(
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            List<int> expiredBookingIds;

            // Use a short-lived read scope so entities discovered before the
            // transaction are never reused as stale tracked instances.
            using (var readScope = _scopeFactory.CreateScope())
            {
                var readUnitOfWork =
                    readScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                expiredBookingIds = (
                    await readUnitOfWork.Bookings.GetExpiredPendingIdsAsync(
                        now,
                        cancellationToken))
                    .Distinct()
                    .ToList();
            }

            if (expiredBookingIds.Count == 0)
                return;

            // A new scope gives the transaction a fresh DbContext. Every
            // booking is re-read inside the Serializable transaction before
            // changing its status.
            using var processingScope = _scopeFactory.CreateScope();
            var unitOfWork =
                processingScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var waitlistService =
                processingScope.ServiceProvider.GetRequiredService<IWaitlistService>();

            await unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    var rejectedBookingIds = new List<int>();

                    foreach (int bookingId in expiredBookingIds)
                    {
                        var booking = await unitOfWork.Bookings.GetByIdAsync(
                            bookingId,
                            ct);

                        DateTime currentTime = DateTime.UtcNow;

                        if (booking is null
                            || booking.Status != BookingStatus.Pending
                            || booking.StartTime > currentTime)
                        {
                            continue;
                        }

                        booking.ExpirePending(
                            "Yêu cầu booking đã quá giờ bắt đầu và tự động bị từ chối.");

                        unitOfWork.Bookings.Update(booking);
                        rejectedBookingIds.Add(booking.BookingId);

                        await unitOfWork.Notifications.AddAsync(
                            new Notification(
                                booking.UserId,
                                "Yêu cầu booking đã hết hạn",
                                $"Booking #{booking.BookingId} đã quá giờ bắt đầu "
                                + "nhưng chưa được duyệt nên hệ thống tự động từ chối.",
                                NotificationType.BookingRejected),
                            ct);
                    }

                    if (rejectedBookingIds.Count == 0)
                        return;

                    // Persist Rejected status inside the current transaction so
                    // WaitlistService sees the released resource.
                    await unitOfWork.SaveChangesAsync(ct);

                    foreach (int bookingId in rejectedBookingIds)
                    {
                        await waitlistService.NotifyNextForReleasedBookingAsync(
                            bookingId,
                            ct);
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }
    }
}
