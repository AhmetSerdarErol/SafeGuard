using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
namespace SafeGuard.Mobile
{
    public partial class DashboardPage : ContentPage
    {
        private bool isCountingDown = false;
        private bool isCooldown = false;
        private CancellationTokenSource _cancelTokenSource;

        public DashboardPage()
        {
            InitializeComponent();
            // Başlangıçta Kırmızı Radar Çalışsın
            StartRedPulse();
        }

        // --- 1. KIRMIZI RADAR (DIŞA DOĞRU) ---
        private void StartRedPulse()
        {
            // Önce varsa diğer animasyonu durdur
            this.AbortAnimation("PulseEffect");

            // Rengi Kırmızı Yap
            PulsingRing.Stroke = Color.FromArgb("#FF0000"); // Kırmızı

            var pulseAnimation = new Animation();

            // Dışa büyüme (1.0 -> 1.5)
            var scaleUp = new Animation(v => PulsingRing.Scale = v, 1, 1.5);
            // Kaybolma (Opacity 0.8 -> 0)
            var fadeOut = new Animation(v => PulsingRing.Opacity = v, 0.8, 0);

            pulseAnimation.Add(0, 1, scaleUp);
            pulseAnimation.Add(0, 1, fadeOut);

            pulseAnimation.Commit(this, "PulseEffect", 16, 2000, Easing.CubicOut, (v, c) =>
            {
                PulsingRing.Scale = 1;
                PulsingRing.Opacity = 0.8;
            }, () => true);
        }

        // --- 2. YEŞİL KALKAN (İÇE DOĞRU) ---
        private void StartGreenPulse()
        {
            // Kırmızı animasyonu durdur
            this.AbortAnimation("PulseEffect");

            // Rengi Yeşil Yap
            PulsingRing.Stroke = Colors.Green; // Yeşil

            var successAnimation = new Animation();

            // Dışarıdan (1.6) İçeriye (1.0) küçülme
            var scaleDown = new Animation(v => PulsingRing.Scale = v, 1.6, 1);
            // Görünür olma (Opacity 0 -> 0.8) - Sanki dışarıdan enerji topluyor gibi
            var fadeIn = new Animation(v => PulsingRing.Opacity = v, 0, 0.8);

            successAnimation.Add(0, 1, scaleDown);
            successAnimation.Add(0, 1, fadeIn);

            // "SuccessEffect" adıyla çalıştır
            successAnimation.Commit(this, "SuccessEffect", 16, 2000, Easing.SinOut, (v, c) =>
            {
                PulsingRing.Scale = 1.6;
                PulsingRing.Opacity = 0;
            }, () => true);
        }

        // Tıklama Olayı
        private async void OnSosTapped(object sender, EventArgs e)
        {
            if (isCooldown) return; // Kilitliyse tepki verme

            if (isCountingDown)
            {
                CancelSosProcess();
            }
            else
            {
                await StartSosCountdown();
            }
        }

        private async Task StartSosCountdown()
        {
            isCountingDown = true;
            _cancelTokenSource = new CancellationTokenSource();
            var token = _cancelTokenSource.Token;

            // Görsel Değişiklikler
            SosLabel.Text = "İPTAL";
            SosLabel.FontSize = 30;
            SosBtnBorder.BackgroundColor = Colors.Gray;
            StatusLabel.Text = "GÖNDERİLİYOR...\nDURDURMAK İÇİN DOKUN!";
            StatusLabel.TextColor = Colors.Orange;

            try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

            var stopwatch = Stopwatch.StartNew();
            double totalDuration = 3000.0;

            try
            {
                while (stopwatch.ElapsedMilliseconds < totalDuration)
                {
                    if (token.IsCancellationRequested) return;

                    double percent = stopwatch.ElapsedMilliseconds / totalDuration;
                    SosProgress.Progress = percent;
                    await Task.Delay(15);
                }

                if (!token.IsCancellationRequested)
                {
                    SosProgress.Progress = 1;
                    TriggerSos();
                }
            }
            catch (Exception)
            {
                CancelSosProcess();
            }
        }

        private void CancelSosProcess()
        {
            if (isCooldown) return;

            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
            {
                _cancelTokenSource.Cancel();
            }
            isCountingDown = false;

            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(SosProgress);
            SosProgress.Progress = 0;

            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS";
            SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM İÇİN DOKUN";
            StatusLabel.TextColor = Color.FromArgb("#D32F2F");
        }

        private async void TriggerSos()
        {
            isCountingDown = false;
            isCooldown = true;

            // --- BURADA YEŞİL ANİMASYONA GEÇİYORUZ ---
            StartGreenPulse();
            // ------------------------------------------

            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            SosBtnBorder.BackgroundColor = Colors.Green;
            SosLabel.Text = "OK";
            StatusLabel.Text = "YARDIM GÖNDERİLDİ!";
            StatusLabel.TextColor = Colors.Green;
            SosProgress.Progress = 1;

            await DisplayAlert("BAŞARILI", "Acil durum çağrısı ve konum bilgisi iletildi.", "TAMAM");

            int beklemeSuresi = 15;

            for (int i = beklemeSuresi; i > 0; i--)
            {
                StatusLabel.Text = $"YARDIM ÇAĞRILDI.\nEKRAN {i} SN SONRA SIFIRLANACAK.";
                await Task.Delay(1000);
            }

            isCooldown = false;
            ResetScreen();
        }

        private void ResetScreen()
        {
            // --- YEŞİLİ KAPAT, KIRMIZIYA DÖN ---
            this.AbortAnimation("SuccessEffect"); // Yeşili durdur
            StartRedPulse(); // Kırmızıyı başlat
            // -----------------------------------

            isCountingDown = false;
            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(SosProgress);
            SosProgress.Progress = 0;

            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS";
            SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM ÇAĞRILDI! TEKRAR TEKRARDAN YARDIM GÖNDERMEK İÇİN DOKUN";
            StatusLabel.TextColor = Color.FromArgb("#D32F2F");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var token = await SecureStorage.Default.GetAsync("auth_token");

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                // --- DEDEKTİF KODU: Çıktı penceresini kontrol et ---
                foreach (var claim in jsonToken.Claims)
                {
                    System.Diagnostics.Debug.WriteLine($"CLAIM TİPİ: {claim.Type} - DEĞER: {claim.Value}");
                }
                // --------------------------------------------------

                // İsim arama skalasını genişletelim
                var userName = jsonToken?.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                               ?? jsonToken?.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                               ?? jsonToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                               ?? jsonToken?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value
                               ?? "Kullanıcı";

                WelcomeLabel.Text = $"Hoş geldin, {userName}";
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // 1. Hafızadaki Token'ı sil (Kimliği yok et)
            SecureStorage.Default.Remove("auth_token");

            // 2. Giriş sayfasına geri şutla
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }

    }
}