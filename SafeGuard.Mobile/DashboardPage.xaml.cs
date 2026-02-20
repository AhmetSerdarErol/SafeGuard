using System.Diagnostics;
using System.Globalization;
using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;
using SafeGuard.Mobile.Views;
#if ANDROID
using Android.Telephony;
using Android.Content;
#endif

namespace SafeGuard.Mobile
{
    public partial class DashboardPage : ContentPage
    {
        // ARTIK ContactModel KULLANIYORUZ
        private List<ContactModel> _localContacts = new List<ContactModel>();

        private readonly AuthService _authService = new AuthService();
        private readonly ContactService _contactService = new ContactService();
        private readonly SignalRService _signalRService;
        private readonly LocationService _locationService = new LocationService();
        private int currentUserId;
        private bool isCountingDown = false;
        private bool isCooldown = false;
        private bool isSosActive = false;
        private bool _isAlertActive = false;
        private CancellationTokenSource? _cancelTokenSource;

        public DashboardPage()
        {
            InitializeComponent();

            // Converter Kontrolü
            if (!this.Resources.ContainsKey("InitialsConverter"))
            {
                this.Resources.Add("InitialsConverter", new InitialsConverter());
            }

            // SignalR Başlat
            _signalRService = new SignalRService();

            // --- EVENT ABONELİKLERİ ---
            _signalRService.OnSosReceived += HandleIncomingSos;
            _signalRService.OnHelpConfirmed += HandleHelpConfirmation;
            _signalRService.OnSafeReceived += HandleIncomingSafe; // <--- YENİ EKLENDİ (Güvendeyim bildirimi için)

            StartRedPulse();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            currentUserId = Preferences.Get("CurrentUserId", 0);

            string photoUrl = Preferences.Get("UserPhotoUrl", "");
            string fullName = Preferences.Get("UserFullName", "U");

            if (!string.IsNullOrEmpty(photoUrl))
            {
                BottomProfileImage.Source = $"http://10.0.2.2:5161/{photoUrl}";
                BottomInitialsLabel.IsVisible = false;
            }
            else
            {
                BottomProfileImage.Source = null;
                BottomInitialsLabel.IsVisible = true;
                BottomInitialsLabel.Text = fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : fullName.Substring(0, 1).ToUpper();
            }

            UpdateWelcomeMessage();
            await LoadContacts(); // Kişileri Yükle
            await UpdateBadge();  // Bildirimleri Güncelle

            if (currentUserId != 0)
            {
                // Bağlanırken sunucuya "Ben bu ID'li kullanıcıyım" diyoruz
                await _signalRService.ConnectAsync(currentUserId);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Sayfadan çıkınca abonelikleri temizlemek iyi bir pratiktir (Opsiyonel ama önerilir)
            // _signalRService.OnSafeReceived -= HandleIncomingSafe;
        }

        // --- 1. YAKINLARI YÜKLEME METODU ---
        private async Task LoadContacts()
        {
            try
            {
                if (currentUserId != 0)
                {
                    // Servisten ContactModel listesi dönmeli
                    var contacts = await _contactService.GetContactsAsync(currentUserId);

                    _localContacts = contacts;
                    ContactsCollection.ItemsSource = _localContacts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Liste Hatası: {ex.Message}");
            }
        }

        // --- 2. LİSTEDEN SEÇİM YAPINCA ---
        private async void OnContactSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ContactModel c)
            {
                // Seçimi temizle (aynı kişiye tekrar tıklanabilmesi için)
                ((CollectionView)sender).SelectedItem = null;

                // Veriler boşsa "Belirtilmemiş" yazsın
                string kanGrubu = string.IsNullOrEmpty(c.BloodType) ? "Belirtilmemiş" : c.BloodType;
                string dogumTarihi = string.IsNullOrEmpty(c.BirthDate) ? "Belirtilmemiş" : c.BirthDate;

                string message = $"📞 Telefon: {c.PhoneNumber}\n\n" +
                                 $"🩸 Kan Grubu: {kanGrubu}\n\n" +
                                 $"🎂 Doğum Tarihi: {dogumTarihi}";

                await DisplayAlert($"{c.Name} Bilgileri", message, "Kapat");
            }
        }

        // --- MENÜ BUTONLARI ---
        private void OnHomeClicked(object sender, EventArgs e)
        {
            SosView.IsVisible = true;
            ContactsView.IsVisible = false;
        }

        private async void OnMyContactsClicked(object sender, EventArgs e)
        {
            SosView.IsVisible = false;
            ContactsView.IsVisible = true;
            await LoadContacts(); // Listeyi yenile
        }

        private async void OnAddFriendClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddFriendPage());
        }

        private async void OnRequestsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RequestsPage());
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool cevap = await DisplayAlert("Çıkış", "Çıkış yapmak istiyor musunuz?", "Evet", "Hayır");
            if (cevap)
            {
                Preferences.Clear();
                Application.Current.MainPage = new NavigationPage(new MainPage());
            }
        }

        // --- SOS (ACİL DURUM) MANTIĞI ---
        private async void OnSosClicked(object sender, EventArgs e)
        {
            // Eğer SOS zaten aktifse, butona basınca "Güvendeyim" metoduna gider
            if (isSosActive) { await MarkAsSafe(); return; }

            if (isCooldown) return;
            if (isCountingDown) CancelSosProcess(); else await StartSosCountdown();
        }

        private async Task StartSosCountdown()
        {
            isCountingDown = true;
            _cancelTokenSource = new CancellationTokenSource();
            var token = _cancelTokenSource.Token;

            SosLabel.Text = "İPTAL";
            SosLabel.FontSize = 30;
            SosBtnBorder.BackgroundColor = Colors.Gray;
            StatusLabel.Text = "GÖNDERİLİYOR...";
            StatusLabel.TextColor = Colors.Orange;

            try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                while (stopwatch.ElapsedMilliseconds < 3000)
                {
                    if (token.IsCancellationRequested) return;
                    SosProgress.Progress = stopwatch.ElapsedMilliseconds / 3000.0;
                    await Task.Delay(15);
                }
                if (!token.IsCancellationRequested)
                {
                    SosProgress.Progress = 1;
                    await TriggerSos();
                }
            }
            catch { CancelSosProcess(); }
        }

        private void CancelSosProcess()
        {
            if (isCooldown || isSosActive) return;
            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
                _cancelTokenSource.Cancel();

            isCountingDown = false;
            SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS";
            SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM ÇAĞIRMAK İÇİN DOKUN";
            StatusLabel.TextColor = Colors.Gray;
            StartRedPulse();
        }

        private async Task TriggerSos()
        {
            isCountingDown = false;
            isCooldown = true;
            isSosActive = true;

            StartGreenPulse();
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            SosBtnBorder.BackgroundColor = Colors.Green;
            SosLabel.Text = "GÜVENDEYİM";
            SosLabel.FontSize = 20;

            // --- SOS GÖNDERME KISMI ---
            try
            {
                StatusLabel.Text = "KONUM ALINIYOR...";

                // 1. Konumu Bul
                var location = await _locationService.GetCurrentLocationAsync();

                if (location != null)
                {
                    StatusLabel.Text = "SUNUCUYA İLETİLİYOR...";
                    if (currentUserId != 0)
                    {
                        // SignalR ile backend'e konum ve SOS bilgisini atıyoruz
                        await _signalRService.SendSosAsync(currentUserId, location.Latitude, location.Longitude);
                        StatusLabel.Text = "YARDIM ÇAĞRISI YAPILDI!";
                    }
                }
                else
                {
                    StatusLabel.Text = "KONUM BULUNAMADI!";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "HATA OLUŞTU";
                Console.WriteLine($"SOS Hatası: {ex.Message}");
            }
            // ---------------------------------------

            // Döngü burada devam eder, kullanıcı "Güvendeyim" diyene kadar.
            while (isSosActive) { await Task.Delay(2000); }
        }

        // --- GÜVENDEYİM (SOS İPTAL) MANTIĞI ---
        private async Task MarkAsSafe()
        {
            bool answer = await DisplayAlert("Güvende misin?", "Acil durum modunu kapatmak istiyor musunuz?", "EVET", "HAYIR");
            if (answer)
            {
                // 1. Backend'e "Ben Güvendeyim" sinyali gönder
                // Böylece arkadaşlarının telefonuna bildirim gidecek.
                if (currentUserId != 0)
                {
                    await _signalRService.SendSafeAsync(currentUserId);
                }

                // 2. Ekranı sıfırla
                isSosActive = false;
                isCooldown = false;
                ResetScreen();
            }
        }

        private void ResetScreen()
        {
            this.AbortAnimation("SuccessEffect");
            StartRedPulse();
            isCountingDown = false;
            SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS";
            SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM ÇAĞIRMAK İÇİN DOKUN";
            StatusLabel.TextColor = Colors.Gray;
        }

        // --- YARDIMCI METOTLAR ---
        private void UpdateWelcomeMessage()
        {
            WelcomeLabel.Text = $"{Preferences.Get("UserFullName", "Kullanıcı")}";
        }

        private async Task UpdateBadge()
        {
            if (currentUserId == 0) return;
            var requests = await _authService.GetPendingRequestsAsync(currentUserId);
            if (requests != null && requests.Count > 0)
            {
                BadgeContainer.IsVisible = true;
                BadgeLabel.Text = requests.Count.ToString();
            }
            else
            {
                BadgeContainer.IsVisible = false;
            }
        }

        private void StartRedPulse()
        {
            this.AbortAnimation("PulseEffect");
            PulsingRing.Stroke = Color.FromArgb("#FF0000");
            var pulseAnimation = new Animation();
            pulseAnimation.Add(0, 1, new Animation(v => PulsingRing.Scale = v, 1, 1.5));
            pulseAnimation.Add(0, 1, new Animation(v => PulsingRing.Opacity = v, 0.8, 0));
            pulseAnimation.Commit(this, "PulseEffect", 16, 2000, Easing.CubicOut, (v, c) => { PulsingRing.Scale = 1; PulsingRing.Opacity = 0.8; }, () => true);
        }

        private void StartGreenPulse()
        {
            this.AbortAnimation("PulseEffect");
            PulsingRing.Stroke = Colors.Green;
            var successAnimation = new Animation();
            successAnimation.Add(0, 1, new Animation(v => PulsingRing.Scale = v, 1.6, 1));
            successAnimation.Add(0, 1, new Animation(v => PulsingRing.Opacity = v, 0, 0.8));
            successAnimation.Commit(this, "SuccessEffect", 16, 2000, Easing.SinOut, (v, c) => { PulsingRing.Scale = 1.6; PulsingRing.Opacity = 0; }, () => true);
        }

        // --- SIGNALR DİNLEYİCİLERİ ---

        // 1. SOS ALININCA ÇALIŞIR
        private void HandleIncomingSos(string senderIdString, string serverSenderName, double lat, double lng)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_isAlertActive) return;

                // Telefon titreşsin ve ses çıkarsın (Dikkat çekmek için)
                try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

                // Ekrana ACİL DURUM uyarısı çıkar
                bool yardimEt = await DisplayAlert("⚠️ ACİL DURUM!",
                    $"{serverSenderName} yardım istedi!\nKonum: {lat}, {lng}\nOna gitmek istiyor musun?",
                    "GİT (NAVİGASYON)", "İPTAL");

                if (yardimEt)
                {
                    // A. Sunucuya "Tamam, yola çıktım" haberini ver
                    await _signalRService.ConfirmHelp("Bir Dost", senderIdString);

                    // B. HARİTAYI AÇ VE ROTA ÇİZ 🗺️
                    try
                    {
                        var location = new Location(lat, lng);
                        var options = new MapLaunchOptions
                        {
                            Name = serverSenderName, // Haritada arkadaşının adı yazsın
                            NavigationMode = NavigationMode.Driving // Araba moduyla aç
                        };

                        await Map.OpenAsync(location, options);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Hata", "Harita açılamadı: " + ex.Message, "Tamam");
                    }
                }
            });
        }

        // 2. YARDIM ONAYI GELİNCE ÇALIŞIR (Yardım isteyen kişiye)
        private void HandleHelpConfirmation(string helperName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = $"YARDIM GELİYOR: {helperName}";
                StatusLabel.TextColor = Colors.Green;
            });
        }

        // 3. GÜVENDEYİM BİLDİRİMİ GELİNCE ÇALIŞIR (YENİ)
        private void HandleIncomingSafe(string senderName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Arkadaşın güvende olduğunu bildirir
                Application.Current.MainPage.DisplayAlert("✅ DURUM GÜNCELLEMESİ", $"{senderName} şu an güvende olduğunu bildirdi.", "TAMAM");
            });
        }
    }

    // --- CONVERTER (BAŞ HARFLERİ ALMA) ---
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrEmpty(name))
            {
                return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.Substring(0, 1).ToUpper();
            }
            return "?";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}