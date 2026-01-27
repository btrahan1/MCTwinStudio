using CashRecyclerServer.Data;
using CashRecyclerServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CashRecyclerServer.Services
{
    public class DatabaseWatcherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<CashHub> _hubContext;
        private long _lastTotalItems = -1;

        public DatabaseWatcherService(IServiceProvider serviceProvider, IHubContext<CashHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<CashDbContext>();
                        
                        // Calculate "System State Fingerprint"
                        // Sum of all items in all cassettes. If this changes, something happened.
                        // We use Long to avoid overflow, though Int matches the column.
                        var currentTotal = await context.Cassettes.SumAsync(c => (long)c.CurrentCount, stoppingToken);

                        if (_lastTotalItems != -1 && currentTotal != _lastTotalItems)
                        {
                            // State changed!
                            await _hubContext.Clients.All.SendAsync("DashboardUpdate", stoppingToken);
                        }

                        _lastTotalItems = currentTotal;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but keep running
                    Console.WriteLine($"Watcher Error: {ex.Message}");
                }

                // Poll every 5 seconds as requested
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
