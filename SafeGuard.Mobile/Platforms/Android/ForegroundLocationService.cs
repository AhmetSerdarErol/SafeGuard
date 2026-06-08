using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Application = Android.App.Application;

namespace SafeGuard.Platforms.Android
{

    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class ForegroundLocationService : Service
    {
        private const string NOTIFICATION_CHANNEL_ID = "SafeGuardLocationChannel";
        private const int NOTIFICATION_ID = 10001;
        private bool _isServiceRunning = false;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (!_isServiceRunning)
            {
                _isServiceRunning = true;

                StartForegroundService();

                Task.Run(() => LocationTrackingLoop());
            }

            return StartCommandResult.Sticky;
        }

        private void StartForegroundService()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    NOTIFICATION_CHANNEL_ID,
                    "Acil Durum Takibi",
                    NotificationImportance.High);

                channel.Description = "Acil durumlarda konumunuzu yakınlarınızla paylaşır.";

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }

            var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("SafeGuard: SOS Aktif")
                .SetContentText("Konumunuz arka planda yakınlarınızla paylaşılıyor.")
                .SetSmallIcon(Application.Context.ApplicationInfo.Icon) 
                .SetOngoing(true) 
                .Build();

            StartForeground(NOTIFICATION_ID, notification);
        }

        private async Task LocationTrackingLoop()
        {
            using var client = new HttpClient();

            while (_isServiceRunning)
            {
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                    var location = await Geolocation.Default.GetLocationAsync(request);

                    if (location != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[safeguard] konum: {location.Latitude}, {location.Longitude}");

                        var apiUrl = "https://10.0.2.2:7209/api/sos/updatelocation";

                        var payload = new
                        {
                            latitude = location.Latitude,
                            longitude = location.Longitude,
                            timestamp = DateTime.UtcNow
                        };

                        var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
                        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(apiUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine("[safeguard] konum apiye basariyla iletildi");
                        }
                    }
                }
                catch (Exception)
                {
                }

                await Task.Delay(10000);
            }
        }

        public override void OnDestroy()
        {
            _isServiceRunning = false;
            base.OnDestroy();
        }
    }
}