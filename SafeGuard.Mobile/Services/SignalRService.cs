using Microsoft.AspNetCore.SignalR.Client;

namespace SafeGuard.Mobile.Services
{
    public class SignalRService
    {
        private HubConnection _hubConnection;

        // Emülatör için 10.0.2.2, telefon için bilgisayarın IP'sini kullan
        private const string HubUrl = "http://10.0.2.2:5161/sosHub";

        // --- OLAYLAR (Events) ---
        public event Action<string, string, double, double> OnSosReceived;
        public event Action<string> OnHelpConfirmed;
        public event Action<string> OnSafeReceived;  // <-- GÜVENDEYİM DİNLEYİCİSİ

        public async Task ConnectAsync(int userId)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) return;

            try
            {
                var connectionUrl = $"{HubUrl}?userId={userId}";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(connectionUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // --- İŞTE SORDUĞUN KODLAR BURAYA GELİYOR ---

                // 1. SOS Gelirse
                _hubConnection.On<string, string, double, double>("ReceiveSos", (senderId, senderName, lat, lng) =>
                {
                    OnSosReceived?.Invoke(senderId, senderName, lat, lng);
                });

                // 2. Yardım Onayı Gelirse
                _hubConnection.On<string>("HelpConfirmed", (helperName) =>
                {
                    OnHelpConfirmed?.Invoke(helperName);
                });

                // 3. GÜVENDEYİM Mesajı Gelirse (Sorduğun yer)
                _hubConnection.On<string>("ReceiveSafe", (senderName) =>
                {
                    OnSafeReceived?.Invoke(senderName);
                });
                // -------------------------------------------

                await _hubConnection.StartAsync();
                System.Diagnostics.Debug.WriteLine($"SignalR Bağlandı! User: {userId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR Bağlantı Hatası: {ex.Message}");
            }
        }

        // --- GÖNDERME METOTLARI ---
        public async Task SendSosAsync(int userId, double lat, double lng)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("SendSosData", userId, lat, lng);
        }

        public async Task ConfirmHelp(string helperName, string targetUserId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("ConfirmHelp", helperName, targetUserId);
        }

        public async Task SendSafeAsync(int userId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("SendSafeAlert", userId);
        }
    }
}