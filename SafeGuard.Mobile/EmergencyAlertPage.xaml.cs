using SafeGuard.Mobile.RealTime;

namespace SafeGuard.Mobile
{
    public partial class EmergencyAlertPage : ContentPage
    {
        private string _senderId;
        private string _senderName;
        private double _latitude;
        private double _longitude;
        private SignalRService _signalRService;

        // Animasyonu kontrol edecek değişken
        private bool _isPageActive = false;

        public EmergencyAlertPage(string senderId, string senderName, double lat, double lon, SignalRService signalRService)
        {
            InitializeComponent();
            _senderId = senderId;
            _senderName = senderName;
            _latitude = lat;
            _longitude = lon;
            _signalRService = signalRService;

            SenderNameLabel.Text = _senderName;
            LocationLabel.Text = $"Konum: {_latitude:F4}, {_longitude:F4}";

            // BURADAN StartAlarmEffects'i SİLDİK! Artık aşağıda çalışacak.
        }

        // --- SAYFA EKRANA GELİNCE ÇALIŞIR ---
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isPageActive = true; // Sayfa aktif işaretle

            // Ekranın kapanmasını engelle
            DeviceDisplay.Current.KeepScreenOn = true;

            // Animasyonu şimdi başlat! 🚀
            _ = StartPulseAnimation();
        }

        // --- SAYFA GİDİNCE DURUR ---
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isPageActive = false; // Döngüyü kır

            // Ekran kilidini serbest bırak
            DeviceDisplay.Current.KeepScreenOn = false;
        }

        // --- RADAR & NABIZ ANİMASYONU ---
        private async Task StartPulseAnimation()
        {
            // İlk titreşim
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            // Sayfa aktif olduğu sürece dön
            while (_isPageActive)
            {
                // 1. SOS İkonu Büyüsün (Nefes Alma)
                var pulseTask = SosIconBorder.ScaleTo(1.2, 500, Easing.SinOut);

                // 2. Halkaları Sıfırla
                PulseRing1.Scale = 1; PulseRing1.Opacity = 0.8;
                PulseRing2.Scale = 1; PulseRing2.Opacity = 0.6;

                // 3. Halkalar Dışarı Yayılsın (Radar Efekti)
                var ring1Task = Task.WhenAll(
                    PulseRing1.ScaleTo(3.0, 1500, Easing.SinOut),
                    PulseRing1.FadeTo(0, 1500, Easing.SinOut)
                );

                // İkinci halka biraz daha yavaş
                var ring2Task = Task.WhenAll(
                    PulseRing2.ScaleTo(2.5, 1500, Easing.SinOut),
                    PulseRing2.FadeTo(0, 1500, Easing.SinOut)
                );

                // Hepsini oynat
                await Task.WhenAll(pulseTask, ring1Task, ring2Task);

                // İkonu Küçült (Nefes Verme)
                await SosIconBorder.ScaleTo(1.0, 500, Easing.SinIn);

                // Kısa bir bekleme
                await Task.Delay(200);
            }
        }

        private async void OnMapClicked(object sender, EventArgs e)
        {
            try
            {
                await Map.OpenAsync(_latitude, _longitude, new MapLaunchOptions
                {
                    Name = _senderName,
                    NavigationMode = NavigationMode.Driving
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Harita açılamadı: " + ex.Message, "Tamam");
            }
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            string myName = Preferences.Get("UserFullName", "Bir Yardımsever");
            await _signalRService.ConfirmHelp(myName, _senderId);
            await DisplayAlert("Onaylandı", "Yardım bildiriminiz iletildi.", "Tamam");
            await Navigation.PopModalAsync();
        }

        private async void OnIgnoreClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}