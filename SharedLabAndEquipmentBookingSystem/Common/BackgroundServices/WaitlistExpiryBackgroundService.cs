using Application.Interfaces;

namespace API.Common.BackgroundServices
{
    public sealed class WaitlistExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan NotificationLifetime = TimeSpan.FromMinutes(30);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WaitlistExpiryBackgroundService> _logger;

        public WaitlistExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<WaitlistExpiryBackgroundService> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var waitlistService = scope.ServiceProvider
                        .GetRequiredService<IWaitlistService>();

                    await waitlistService.ProcessExpiredNotificationsAsync(
                        DateTime.UtcNow.Subtract(NotificationLifetime),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể xử lý waitlist đã hết thời gian nhận chỗ.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
