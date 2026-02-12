using Microsoft.AspNetCore.SignalR;

namespace SafeGuard.Hubs
{
    public class SosHub : Hub
    {
        // Kullanıcı online olduğunda onu kendi ID'siyle bir odaya alıyoruz
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        // SOS atan kişi, bu metodu tetikleyecek
        public async Task SendSosToContacts(string senderName, List<string> contactIds)
        {
            foreach (var id in contactIds)
            {
                // Sadece listesindeki "Verified" kişilere anlık fişek atar
                await Clients.Group(id).SendAsync("ReceiveSosAlert", senderName);
            }
        }

        // Yardım yolda onayı
        public async Task SendHelpOnTheWay(string helperName, string targetUserId)
        {
            await Clients.Group(targetUserId).SendAsync("ReceiveHelpConfirmation", helperName);
        }
    }
}