using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Devices.Sensors;

namespace SafeGuard.Mobile.Services 
{
    public class EmergencyLocationService
    {
        private HubConnection _hubConnection;
        private bool _isEmergencyActive = false;

        public async Task StartBroadcastingAsync(string myUserId)
        {
            string hubUrl = "http://10.0.2.2:5000/locationHub"; 

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            try
            {
                await _hubConnection.StartAsync();
                _isEmergencyActive = true;
                Console.WriteLine("SOS Fırlatıcı Aktif! Konumlar gönderilmeye başlanıyor...");

                _ = Task.Run(async () =>
                {
                    while (_isEmergencyActive)
                    {
                        try
                        {
                            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(3));
                            var location = await Geolocation.GetLocationAsync(request);

                            if (location != null)
                            {
                                await _hubConnection.SendAsync("SendLocationUpdate", myUserId, location.Latitude, location.Longitude);
                                Console.WriteLine($"Konum fırlatıldı: {location.Latitude}, {location.Longitude}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Konum alınamadı veya gönderilemedi: {ex.Message}");
                        }
                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hub'a bağlanılamadı: {ex.Message}");
            }
        }

        public async Task StopBroadcastingAsync()
        {
            _isEmergencyActive = false;
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }
        }
    }
}