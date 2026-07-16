using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace API.BackgroundServices
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
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            DateTime now = DateTime.UtcNow;

            var expiredBookings = await unitOfWork.Bookings.FindAsync(
                x => x.Status == BookingStatus.Pending && x.StartTime <= now,
                cancellationToken);

            if (expiredBookings.Count == 0)
                return;

            await unitOfWork.ExecuteInSerializableTransactionAsync(
                async ct =>
                {
                    foreach (var booking in expiredBookings)
                    {
                        if (booking.Status != BookingStatus.Pending
                            || booking.StartTime > DateTime.UtcNow)
                        {
                            continue;
                        }

                        booking.ExpirePending(
                            "Yêu cầu booking đã quá giờ bắt đầu và tự động bị từ chối.");
                        unitOfWork.Bookings.Update(booking);

                        await unitOfWork.Notifications.AddAsync(
                            new Notification(
                                booking.UserId,
                                "Yêu cầu booking đã hết hạn",
                                $"Booking #{booking.BookingId} đã quá giờ bắt đầu "
                                + "nhưng chưa được duyệt nên hệ thống tự động từ chối.",
                                NotificationType.BookingRejected),
                            ct);
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken);
        }
    }
}
