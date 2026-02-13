using System.Diagnostics;
using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;
using Contact = SafeGuard.Mobile.Models.Contact;
using SafeGuard.Mobile.RealTime;
#if ANDROID
using Android.Telephony;
using Android.Content;
#endif
namespace SafeGuard.Mobile
{
    public partial class DashboardPage : ContentPage
    {
        private bool isCountingDown = false;
        private bool isCooldown = false;
        private bool isSosActive = false;
        private CancellationTokenSource? _cancelTokenSource;

        // YENİ: Ekranda zaten alarm var mı kontrolü (Spam engelleme)
        private bool _isAlertActive = false;

        // YENİ: Zaten yardım ettiğim kişileri sessize al (Tekrar tekrar sormasın)
        private List<int> _helpedUserIds = new List<int>();

        private readonly AuthService _authService = new AuthService();
        private readonly ContactService _contactService = new ContactService();
        private readonly SignalRService _signalRService;

        private List<Contact> _localContacts = new List<Contact>();

        public DashboardPage()
        {
            InitializeComponent();

            _signalRService = new SignalRService();
            _signalRService.OnSosReceived += HandleIncomingSos;
            _signalRService.OnHelpConfirmed += HandleHelpConfirmation;

            UpdateWelcomeMessage();
            StartRedPulse();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _signalRService.ConnectAsync();
            UpdateWelcomeMessage();
            await LoadContacts();
        }

        // --- DÜZELTİLEN KISIM: Sinyal Gelince ---
        private void HandleIncomingSos(string senderIdString, string serverSenderName)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 1. KONTROL: Ekranda zaten bir alarm varsa yenisini açma!
                if (_isAlertActive) return;

                int currentUserId = Preferences.Get("CurrentUserId", 0);

                // 2. KONTROL: ID'leri çevir
                if (!int.TryParse(senderIdString, out int senderId)) return;

                // 3. KRİTİK KONTROL: Sinyal benden mi geliyor? (Kendime bildirim atmayayım)
                if (senderId == currentUserId) return;

                // 4. KONTROL: Ben bu kişiye zaten "YARDIM ET" dedim mi?
                if (_helpedUserIds.Contains(senderId)) return;

                // --- Buraya geldiyse geçerli bir alarmdır ---
                _isAlertActive = true; // Kilidi kapat

                string gorunecekIsim = serverSenderName;
                var kayitliKisi = _localContacts.FirstOrDefault(c => c.UserId == senderId);
                if (kayitliKisi != null)
                {
                    gorunecekIsim = kayitliKisi.Name;
                }

                // Alarmı Göster
                bool yardimEt = await DisplayAlert("⚠️ ACİL DURUM!",
                    $"{gorunecekIsim} acil yardım çağrısı gönderdi!",
                    "YARDIM ET (YOLDAYIM)", "Görmezden Gel");

                if (yardimEt)
                {
                    // Yardım Edildi listesine ekle ki tekrar bildirim gelmesin
                    _helpedUserIds.Add(senderId);

                    string myName = Preferences.Get("UserFullName", "Birisi");

                    // Onayı gönder
                    await _signalRService.ConfirmHelp(myName, senderIdString);

                    await DisplayAlert("Bilgi", "Yardım bildiriminiz iletildi. Konum açılıyor...", "Tamam");
                }

                // İşlem bitince kilidi aç
                _isAlertActive = false;
            });
        }

        // --- DÜZELTİLEN KISIM: Onay Gelince (Yağmur'un Ekranı) ---
        private void HandleHelpConfirmation(string helperName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Sadece ben SOS modundaysam bu mesajı ciddiye al
                if (isSosActive)
                {
                    StatusLabel.Text = $"YARDIM TALEBİNİZ ONAYLANDI.\n{helperName} size doğru yola çıktı.";
                    StatusLabel.TextColor = Colors.Green;

                    try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }
                }
            });
        }

        private void UpdateWelcomeMessage()
        {
            string savedName = Preferences.Get("UserFullName", "");
            WelcomeLabel.Text = string.IsNullOrEmpty(savedName) ? "Hoş Geldiniz" : $"Hoş Geldiniz, {savedName}";
        }

        private async Task LoadContacts()
        {
            try
            {
                if (ContactsList.Children.Count > 0)
                {
                    var plusButton = ContactsList.Children[0];
                    ContactsList.Children.Clear();
                    ContactsList.Children.Add(plusButton);
                }

                int userId = Preferences.Get("CurrentUserId", 0);
                if (userId != 0)
                {
                    var contacts = await _contactService.GetContactsAsync(userId);
                    _localContacts = contacts;
                    foreach (var contact in contacts) { AddContactToUI(contact); }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Liste Hatası: {ex.Message}"); }
        }

        private async void OnSosTapped(object sender, EventArgs e)
        {
            if (isSosActive)
            {
                await MarkAsSafe();
                return;
            }
            if (isCooldown) return;
            if (isCountingDown) CancelSosProcess();
            else await StartSosCountdown();
        }

        private async Task StartSosCountdown()
        {
            isCountingDown = true;
            _cancelTokenSource = new CancellationTokenSource();
            var token = _cancelTokenSource.Token;

            SosLabel.Text = "İPTAL"; SosLabel.FontSize = 30; SosBtnBorder.BackgroundColor = Colors.Gray;
            StatusLabel.Text = "GÖNDERİLİYOR..."; StatusLabel.TextColor = Colors.Orange;
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
                if (!token.IsCancellationRequested) { SosProgress.Progress = 1; await TriggerSos(); }
            }
            catch { CancelSosProcess(); }
        }

        private void CancelSosProcess()
        {
            if (isCooldown || isSosActive) return;
            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested) _cancelTokenSource.Cancel();
            isCountingDown = false;
            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(SosProgress);
            SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS"; SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM İÇİN 3 SN BASILI TUT"; StatusLabel.TextColor = Color.FromArgb("#D32F2F");
            StartRedPulse();
        }

        private async Task TriggerSos()
        {
            isCountingDown = false; isCooldown = true; isSosActive = true;
            StartGreenPulse();
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            SosBtnBorder.BackgroundColor = Colors.Green;
            SosLabel.Text = "GÜVENDEYİM"; SosLabel.FontSize = 20;

            string myId = Preferences.Get("CurrentUserId", 0).ToString();
            string myName = Preferences.Get("UserFullName", "");

            // Doğrulanmış kişileri hazırla
            var targetContactIds = _localContacts
                .Where(c => c.VerificationStatus == "Verified")
                .Select(c => c.UserId.ToString())
                .ToList();

            // --- YENİ: KONUM ALMA KISMI ---
            double latitude = 41.0082;  // Varsayılan (GPS çekemezse burası gider)
            double longitude = 28.9784;

            try
            {
                // Konumu çekmeye çalış (En fazla 5 saniye bekle)
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    latitude = location.Latitude;
                    longitude = location.Longitude;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPS Hatası: {ex.Message}");
                // Hata olursa varsayılan konumla devam et, uygulamayı çökertme
            }
            // -----------------------------

            try
            {
                // 1. Google Maps Linki Oluştur
                // Örnek: https://maps.google.com/?q=37.7749,29.0876
                // Bunu kullanırsan linke tıklayanın telefonunda direkt Navigasyon açılır:
                string mapLink = $"https://www.google.com/maps/search/?api=1&query={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                // 2. Mesajı Hazırla
                string smsMessage = $"ACIL! {myName} acil durum butonuna basti! Lutfen hemen ulasin veya yardim gonderin. Konum: {mapLink}";

                // 3. Doğrulanmış Kişilere Tek Tek Gönder
                foreach (var contact in _localContacts.Where(c => c.VerificationStatus == "Verified"))
                {
                    // Telefon numarasını temizle (Boşlukları sil)
                    string cleanPhone = contact.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

                    // SMS Metodunu Çağır
                    await SendSmsInBackground(cleanPhone, smsMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SMS Paketi Hatası: " + ex.Message);
            }
            while (isSosActive)
            {
                // ARTIK GERÇEK KONUM GİDİYOR!
                bool success = await _authService.SendSosAlertAsync(latitude, longitude);

                if (targetContactIds.Any())
                {
                    await _signalRService.EmitSos(myId, myName, targetContactIds);
                }

                if (success)
                {
                    if (!StatusLabel.Text.Contains("ONAYLANDI"))
                    {
                        StatusLabel.Text = "YARDIM ÇAĞRISI GÖNDERİLDİ\n(Konum Paylaşılıyor...)";
                        StatusLabel.TextColor = Colors.Green;
                    }
                }
                else
                {
                    StatusLabel.Text = "BAĞLANTI ZAYIF...\nTekrar deneniyor...";
                    StatusLabel.TextColor = Colors.Orange;
                }

                for (int i = 0; i < 30; i++)
                {
                    if (!isSosActive) break;
                    await Task.Delay(1000);
                }
            }
        }

        private async Task MarkAsSafe()
        {
            bool answer = await DisplayAlert("Güvende misin?", "Acil durum modunu kapatmak istiyor musunuz?", "EVET, İYİYİM", "HAYIR");
            if (answer)
            {
                isSosActive = false;
                isCooldown = false;
                _helpedUserIds.Clear(); // Listeyi temizle
                ResetScreen();
                await DisplayAlert("Bilgi", "Durum normale döndü.", "Tamam");
            }
        }

        private void ResetScreen()
        {
            this.AbortAnimation("SuccessEffect"); StartRedPulse();
            isCountingDown = false; SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS"; SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM İÇİN 3 SN BASILI TUT"; StatusLabel.TextColor = Color.FromArgb("#D32F2F");
        }

        // --- Kişi Ekleme UI vs (Aynen Kalabilir) ---
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

        private async void OnAddContactTapped(object sender, EventArgs e)
        {
            int userId = Preferences.Get("CurrentUserId", 0);
            if (userId == 0) return;
            string name = await DisplayPromptAsync("Kişi Ekle", "Ad:", "Ekle", "İptal");
            if (string.IsNullOrWhiteSpace(name)) return;
            string phone = await DisplayPromptAsync("Kişi Ekle", "Tel:", "Kaydet", "İptal", keyboard: Keyboard.Telephone);
            if (string.IsNullOrWhiteSpace(phone)) return;
            var newContact = new Contact { Name = name, PhoneNumber = phone, UserId = userId };
            var added = await _contactService.AddContactAsync(newContact);
            if (added != null) AddContactToUI(added);
        }

        private void AddContactToUI(Contact contact)
        {
            string initials = contact.Name.Length >= 2 ? contact.Name.Substring(0, 2).ToUpper() : contact.Name.Substring(0, 1).ToUpper();
            Color statusColor = contact.VerificationStatus switch { "Verified" => Color.FromArgb("#4CAF50"), "Blocked" => Color.FromArgb("#D32F2F"), _ => Color.FromArgb("#FBC02D") };
            var stack = new VerticalStackLayout { Spacing = 5 };
            var border = new Border { HeightRequest = 65, WidthRequest = 65, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 32.5 }, BackgroundColor = statusColor, Padding = 0 };
            border.Content = new Label { Text = initials, TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            stack.Children.Add(border);
            stack.Children.Add(new Label { Text = contact.Name, TextColor = Colors.White, FontSize = 11, HorizontalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation, MaximumWidthRequest = 70 });
            ContactsList.Children.Add(stack);
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("CurrentUserId");
            Preferences.Remove("UserFullName");
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
        private async void OnTestAlarmClicked(object sender, EventArgs e)
        {
            // Rastgele bir konum (Örn: Galata Kulesi) ve isimle sayfayı açıyoruz
            double testLat = 41.0256;
            double testLon = 28.9741;

            // Sahte bir alarm sayfası aç
            await Navigation.PushModalAsync(new EmergencyAlertPage("999", "TEST KİŞİSİ", testLat, testLon, _signalRService));
        }
        private async Task SendSmsInBackground(string phoneNumber, string message)
        {
#if ANDROID
            try
            {
                // 1. İzin Kontrolü (Kullanıcı izin vermiş mi?)
                var status = await Permissions.CheckStatusAsync<Permissions.Sms>();
                if (status !=PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Sms>();
                }

                if (status ==PermissionStatus.Granted)
                {
                    // 2. Android SMS Yöneticisini Çağır
                    SmsManager smsManager = SmsManager.Default;

                    // 3. Mesajı Gönder (Sessizce)
                    smsManager.SendTextMessage(phoneNumber, null, message, null, null);

                    // Console'a yaz (Test için)
                    System.Diagnostics.Debug.WriteLine($"SMS Gönderildi: {phoneNumber}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS Hatası: {ex.Message}");
            }
#else
    // iOS veya Windows ise burası çalışır (Şimdilik boş)
    await Task.CompletedTask;
#endif
        }
    }
}