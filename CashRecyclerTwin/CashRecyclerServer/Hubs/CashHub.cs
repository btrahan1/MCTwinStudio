using Microsoft.AspNetCore.SignalR;

namespace CashRecyclerServer.Hubs
{
    public class CashHub : Hub
    {
        public async Task SendUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveUpdate", message);
        }
    }
}
