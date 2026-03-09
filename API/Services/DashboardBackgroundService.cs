using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace API.Services
{
    public class DashboardBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardBackgroundService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Cập nhật mỗi 30 giây

        public DashboardBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DashboardBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dashboard Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tạo scope để sử dụng DashboardService (scoped service)
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
                        // Trigger cập nhật dashboard
                        await dashboardService.TriggerRealtimeUpdateAsync();
                    }

                    _logger.LogDebug("Dashboard updated at {Time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating dashboard in background service");
                }

                // Đợi cho đến lần cập nhật tiếp theo hoặc bị cancel
                try
                {
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("Dashboard Background Service stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dashboard Background Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
