using Microsoft.AspNetCore.SignalR;

namespace SalesServer.Hubs
{
    public class WarehouseHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task NotifySale(string sku, int quantity)
        {
             await Clients.All.SendAsync("NewSale", sku, quantity);
        }
    }
}
