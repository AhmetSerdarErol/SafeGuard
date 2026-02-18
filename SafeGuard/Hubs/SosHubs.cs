using Microsoft.AspNetCore.SignalR;
using SafeGuard.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent; // <-- BU EKSİKTİ

namespace SafeGuard.Hubs
{
    public class SosHub : Hub
    {
        private readonly AppDbContext _context;

        // --- İŞTE BU EKSİK OLDUĞU İÇİN HATA ALIYORSUN ---
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        // -------------------------------------------------

        public SosHub(AppDbContext context)
        {
            _context = context;
        }

        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                Console.WriteLine($"KULLANICI BAĞLANDI: {userId}");
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var item = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(item.Key))
            {
                UserConnections.TryRemove(item.Key, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendSosData(int userId, double lat, double lng)
        {
            var senderUser = await _context.Users.FindAsync(userId);
            if (senderUser == null) return;

            var friends = _context.Helpers
                .Where(h => (h.UserId == userId || h.HelperId == userId) && h.Status == "Accepted")
                .ToList();

            foreach (var relation in friends)
            {
                int friendId = (relation.UserId == userId) ? relation.HelperId : relation.UserId;
                if (UserConnections.TryGetValue(friendId.ToString(), out string? connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveSos", userId.ToString(), senderUser.FullName, lat, lng);
                }
            }
            // Test için sana geri bildirim
            await Clients.Caller.SendAsync("ReceiveSos", userId.ToString(), senderUser.FullName, lat, lng);
        }

        public async Task ConfirmHelp(string helperName, string targetUserId)
        {
            if (UserConnections.TryGetValue(targetUserId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("HelpConfirmed", helperName);
            }
        }

        // --- GÜVENDEYİM METODU ---
        public async Task SendSafeAlert(int userId)
        {
            var senderUser = await _context.Users.FindAsync(userId);
            if (senderUser == null) return;

            var friends = _context.Helpers
                .Where(h => (h.UserId == userId || h.HelperId == userId) && h.Status == "Accepted")
                .ToList();

            foreach (var relation in friends)
            {
                int friendId = (relation.UserId == userId) ? relation.HelperId : relation.UserId;
                if (UserConnections.TryGetValue(friendId.ToString(), out string? connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveSafe", senderUser.FullName);
                }
            }
            // Test için sana da gönder
            await Clients.Caller.SendAsync("ReceiveSafe", senderUser.FullName);
        }
    }
}