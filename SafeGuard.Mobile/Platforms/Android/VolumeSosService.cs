using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Views.Accessibility;
using Android.Views;
using SafeGuard.Mobile.Services;
namespace SafeGuard.Mobile.Platforms.Android
{
    [Service(Label = "SafeGuard SOS Servisi", Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE", Exported = false)]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    [MetaData("android.accessibilityservice", Resource = "@xml/accessibility_service_config")]
    public class VolumeSosService : AccessibilityService
    {
        private int _clickCount = 0;
        private DateTime _lastClickTime = DateTime.MinValue;

        public override void OnAccessibilityEvent(AccessibilityEvent e) { }
        public override void OnInterrupt() { }

        protected override bool OnKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.VolumeDown && e.Action == KeyEventActions.Down)
            {
                var now = DateTime.Now;

                if ((now - _lastClickTime).TotalSeconds > 3)
                {
                    _clickCount = 0; 
                }

                _clickCount++;
                _lastClickTime = now;

                System.Diagnostics.Debug.WriteLine($"[SAFEGUARD] Ses kısma tuşuna basıldı! Sayı: {_clickCount}");

                if (_clickCount == 5)
                {
                    _clickCount = 0;
                    TriggerSos();
                    return true; 
                }

                return false; 
            }

            return base.OnKeyEvent(e);
        }

        private void TriggerSos()
        {
            System.Diagnostics.Debug.WriteLine("🚨 5 KERE BASILDI! SES TUŞUYLA SOS TETİKLENDİ! 🚨");

            Microsoft.Maui.Storage.Preferences.Default.Set("IsSosActiveState", true);

            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromSeconds(2));
            }
            catch { }

            Task.Run(async () =>
            {
                try
                {
                    int currentUserId = Microsoft.Maui.Storage.Preferences.Get("CurrentUserId", 0);
                    if (currentUserId == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[SAFEGUARD] Kullanıcı ID bulunamadı, işlem iptal!");
                        return;
                    }
                    var locationService = new LocationService();
                    var authService = new AuthService();
                    var signalRService = new SignalRService();
                    var emergencyLocationService = new EmergencyLocationService();

                    // Konumu al
                    System.Diagnostics.Debug.WriteLine("[SAFEGUARD] Konum alınıyor...");

                    Location location = null;
                    try
                    {
                        location = await locationService.GetCurrentLocationAsync().WaitAsync(TimeSpan.FromSeconds(4));
                    }
                    catch
                    {
                        location = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLastKnownLocationAsync();
                    }

                    if (location == null) location = new Location(0, 0);

                    if (location != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SAFEGUARD] Konum bulundu: {location.Latitude}, {location.Longitude}");

                        bool fuzeGittimi = await authService.SendSosAlertAsync(currentUserId, location.Latitude, location.Longitude);

                        if (fuzeGittimi)
                        {
                            System.Diagnostics.Debug.WriteLine("=== 🚀 ARKA PLANDAN FÜZE ATEŞLENDİ! SOS BAŞARILI! ===");

                            await signalRService.ConnectAsync(currentUserId);
                            await signalRService.SendSosAsync(currentUserId, location.Latitude, location.Longitude);
                            _ = emergencyLocationService.StartBroadcastingAsync(currentUserId.ToString());
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[SAFEGUARD] API Reddedildi! Token eksik veya sunucu hatası.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[SAFEGUARD] Konum bilgisine ulaşılamadı!");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SAFEGUARD] Arka plan API gönderme hatası: {ex.Message}");
                }
            });
        }
    }
}