using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace SafeGuard.Mobile.RealTime
{
    public class SignalRService
    {
        private HubConnection _hubConnection;
        // Kendi IP adresini kontrol et (Emulator: 10.0.2.2, Gerçek Cihaz: Bilgisayar IP'si)
        private readonly string _hubUrl = "http://10.0.2.2:5161/sosHub";

        // Olaylar: (GönderenID, GönderenIsim)
        public event Action<string, string> OnSosReceived;
        public event Action<string> OnHelpConfirmed;

        public SignalRService()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Sunucudan gelen SOS sinyalini dinle (ID ve İsim ile)
            _hubConnection.On<string, string>("ReceiveSosAlert", (senderId, senderName) =>
            {
                OnSosReceived?.Invoke(senderId, senderName);
            });

            // Yardım onayını dinle
            _hubConnection.On<string>("ReceiveHelpConfirmation", (helperName) =>
            {
                OnHelpConfirmed?.Invoke(helperName);
            });
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection.State == HubConnectionState.Connected) return;

            try
            {
                await _hubConnection.StartAsync();

                // Kullanıcı bağlanınca kendi ID'siyle odaya katılsın
                int userId = Preferences.Get("CurrentUserId", 0);
                if (userId != 0)
                {
                    await _hubConnection.SendAsync("JoinUserGroup", userId.ToString());
                }

                Debug.WriteLine("SignalR: Bağlandı ✅");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SignalR Hatası: {ex.Message}");
            }
        }

        // SOS Gönderme (Backend'deki metoda ID ve İsim gönderir)
        public async Task EmitSos(string senderId, string senderName, List<string> contactIds)
        {
            if (_hubConnection.State != HubConnectionState.Connected) await ConnectAsync();

            try
            {
                await _hubConnection.SendAsync("SendSosToContacts", senderId, senderName, contactIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SOS Emit Hatası: {ex.Message}");
            }
        }

        // Yardım Ediyorum Onayı
        public async Task ConfirmHelp(string helperName, string targetUserId)
        {
            if (_hubConnection.State != HubConnectionState.Connected) await ConnectAsync();

            try
            {
                await _hubConnection.SendAsync("SendHelpOnTheWay", helperName, targetUserId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Onay Gönderme Hatası: {ex.Message}");
            }
        }
    }
}