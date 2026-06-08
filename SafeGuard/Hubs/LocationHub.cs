using Microsoft.AspNetCore.SignalR;

namespace SafeGuardAPI.Hubs 
{
    public class LocationHub : Hub
    {
        public async Task SendLocationUpdate(string userId, double latitude, double longitude)
        {       
            await Clients.All.SendAsync("ReceiveLocationUpdate", userId, latitude, longitude);
        }
    }
}