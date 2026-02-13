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
        public async Task SendSosToContacts(string senderId, string senderName, List<string> contactIds)
        {
            foreach (var id in contactIds)
            {
                // ARTIK HEM ID HEM İSİM GÖNDERİYORUZ
                await Clients.Group(id).SendAsync("ReceiveSosAlert", senderId, senderName);
            }
        }

        // Yardım yolda onayı
        public async Task SendHelpOnTheWay(string helperName, string targetUserId)
        {
            await Clients.Group(targetUserId).SendAsync("ReceiveHelpConfirmation", helperName);
        }
    }
}