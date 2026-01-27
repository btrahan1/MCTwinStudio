using Microsoft.AspNetCore.SignalR;
using SalesServer.Data;
using SalesServer.Hubs;

namespace SalesServer.Services
{
    public class SalesPollerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<WarehouseHub> _hubContext;
        private readonly ILogger<SalesPollerService> _logger;
        private int _lastProcessedId = 0;

        public SalesPollerService(IServiceScopeFactory scopeFactory, IHubContext<WarehouseHub> hubContext, ILogger<SalesPollerService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sales Poller Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SalesContext>();
                        
                        // Find unprocessed sales
                        var newSales = context.Sales
                            .Where(s => s.Id > _lastProcessedId)
                            .OrderBy(s => s.Id)
                            .ToList();

                        foreach (var sale in newSales)
                        {
                            _logger.LogInformation($"Processing Sale ID: {sale.Id}, SKU: {sale.ProductSku}, Qty: {sale.Quantity}");
                            
                            // Broadcast event via SignalR
                            await _hubContext.Clients.All.SendAsync("NewSale", sale.ProductSku, sale.Quantity);

                            _lastProcessedId = sale.Id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing sales.");
                }

                await Task.Delay(5000, stoppingToken); // Poll every 5 seconds
            }
        }
    }
}
