using Domain;
using Domain.Entities;
using Domain.Interfaces;

namespace API.BackgroundServices
{
    public class BookingReminderBackgroundService
        : BackgroundService
    {
        private static readonly TimeSpan CheckInterval =
            TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingReminderBackgroundService> _logger;

        public BookingReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingReminderBackgroundService> logger)
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
                    await SendUpcomingBookingRemindersAsync(
                        stoppingToken);
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
                        "Không thể tạo thông báo nhắc lịch booking.");
                }

                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
        }

        private async Task SendUpcomingBookingRemindersAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var unitOfWork =
                scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();

            DateTime now = DateTime.UtcNow;

            // Nhắc các booking bắt đầu sau khoảng 25-35 phút.
            DateTime from = now.AddMinutes(25);
            DateTime to = now.AddMinutes(35);

            var bookings =
                await unitOfWork.Bookings.GetCalendarAsync(
                    from,
                    to,
                    null,
                    null,
                    cancellationToken);

            bool hasChanges = false;

            foreach (var booking in bookings
                         .Where(x =>
                             x.Status == BookingStatus.Approved
                             && x.StartTime >= from
                             && x.StartTime <= to))
            {
                string title =
                    $"Nhắc lịch booking #{booking.BookingId}";

                bool alreadySent =
                    await unitOfWork.Notifications.ExistsAsync(
                        x => x.UserId == booking.UserId
                             && x.NotificationType
                                == NotificationType.BookingReminder
                             && x.Title == title,
                        cancellationToken);

                if (alreadySent)
                {
                    continue;
                }

                var notification = new Notification(
                    booking.UserId,
                    title,
                    $"Booking của bạn sẽ bắt đầu lúc "
                    + $"{booking.StartTime:yyyy-MM-dd HH:mm}.",
                    NotificationType.BookingReminder);

                await unitOfWork.Notifications.AddAsync(
                    notification,
                    cancellationToken);

                hasChanges = true;
            }

            if (hasChanges)
            {
                await unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }
    }
}
