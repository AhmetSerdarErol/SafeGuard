using System.Diagnostics;
using System.Globalization;
using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;
using SafeGuard.Mobile.Views;
using Plugin.Firebase.CloudMessaging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

#if ANDROID
using Android.Telephony;
using Android.Content;
#endif

namespace SafeGuard.Mobile
{
    public partial class DashboardPage : ContentPage
    {
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

        // --- HARİTA TAKİP DEĞİŞKENLERİ ---
        private string? _activeTrackingUserId = null;
        private string _activeTrackingUserName = "";

        public DashboardPage()
        {
            InitializeComponent();

            if (!this.Resources.ContainsKey("InitialsConverter"))
            {
                this.Resources.Add("InitialsConverter", new InitialsConverter());
            }

            _signalRService = new SignalRService();

            _signalRService.OnSosReceived += HandleIncomingSos;
            _signalRService.OnHelpConfirmed += HandleHelpConfirmation;
            _signalRService.OnSafeReceived += HandleIncomingSafe;
            CrossFirebaseCloudMessaging.Current.NotificationTapped += (sender, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var data = e.Notification.Data; // Bildirimin içine gizlenmiş veriler

                    // Eğer Firebase'den gelen bildirimde Enlem ve Boylam varsa...
                    if (data.ContainsKey("latitude") && data.ContainsKey("longitude"))
                    {
                        // Bilgileri güvenli bir şekilde çekiyoruz
                        string vName = data.ContainsKey("userName") ? data["userName"].ToString() : "Yardım İsteyen";
                        string vId = data.ContainsKey("userId") ? data["userId"].ToString() : "0";

                        // Türkiye'deki virgüllü sayı çakışmalarını önlemek için Replace koyuyoruz (Jilet gibi çalışır)
                        double lat = Convert.ToDouble(data["latitude"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                        double lng = Convert.ToDouble(data["longitude"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);

                        // İşte O Altın Vuruş: Gizli Harita Katmanını Mermi Gibi Aç!
                        OpenOrUpdateEmergencyMap(vId, vName, lat, lng);
                    }
                    else
                    {
                        // Eğer Backend bildirim yollarken içine koordinat koymadıysa bizi uyarsın
                        Application.Current.MainPage.DisplayAlert("Hata", "Bildirim geldi ancak içinde konum verisi (latitude/longitude) yok. Backend'in bildirim veri yükünü (Data Payload) kontrol et!", "Tamam");
                    }
                });
            };

        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            StartRedPulse();
            currentUserId = Preferences.Get("CurrentUserId", 0);

            string photoUrl = Preferences.Get("UserPhotoUrl", "");
            string fullName = Preferences.Get("UserFullName", "U");

            if (!string.IsNullOrEmpty(photoUrl))
            {
                BottomProfileImage.Source = $"http://10.241.192.15:5161/{photoUrl}";
                BottomInitialsLabel.IsVisible = false;
            }
            else
            {
                BottomProfileImage.Source = null;
                BottomInitialsLabel.IsVisible = true;
                BottomInitialsLabel.Text = fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : fullName.Substring(0, 1).ToUpper();
            }

            UpdateWelcomeMessage();
            await LoadContacts();
            await UpdateBadge();

            if (currentUserId != 0)
            {
                await _signalRService.ConnectAsync(currentUserId);

                try
                {
                    var token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                    if (!string.IsNullOrEmpty(token))
                    {
                        var authService = new AuthService();
                        bool isSaved = await authService.UpdateFcmTokenAsync(currentUserId, token);

                        if (isSaved)
                        {
                            Console.WriteLine("\n=== BAŞARILI: Token veritabanına kaydedildi! ===\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token veritabanına gönderilirken hata oluştu: {ex.Message}");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private async Task LoadContacts()
        {
            try
            {
                if (currentUserId != 0)
                {
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

        private async void OnContactSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ContactModel c)
            {
                ((CollectionView)sender).SelectedItem = null;

                string kanGrubu = string.IsNullOrEmpty(c.BloodType) ? "Belirtilmemiş" : c.BloodType;
                string dogumTarihi = string.IsNullOrEmpty(c.BirthDate) ? "Belirtilmemiş" : c.BirthDate;

                string message = $"📞 Telefon: {c.PhoneNumber}\n\n" +
                                 $"🩸 Kan Grubu: {kanGrubu}\n\n" +
                                 $"🎂 Doğum Tarihi: {dogumTarihi}";

                await DisplayAlert($"{c.Name} Bilgileri", message, "Kapat");
            }
        }

        private void OnHomeClicked(object sender, EventArgs e)
        {
            SosView.IsVisible = true;
            ContactsView.IsVisible = false;
        }

        private async void OnMyContactsClicked(object sender, EventArgs e)
        {
            SosView.IsVisible = false;
            ContactsView.IsVisible = true;
            await LoadContacts();
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

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new NavigationPage(new MainPage());
                });
            }
        }

        private async void OnSosClicked(object sender, EventArgs e)
        {
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

            bool planASuccessful = false;
            bool planBExecuted = false;

            StartGreenPulse();
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            SosBtnBorder.BackgroundColor = Colors.Green;
            SosLabel.Text = "GÜVENDEYİM";
            SosLabel.FontSize = 20;

            while (isSosActive)
            {
                if (!planASuccessful && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        StatusLabel.Text = "KONUM VE KİMLİK ALINIYOR...";

                        await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                        var token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                        var location = await _locationService.GetCurrentLocationAsync();

                        if (location != null && !string.IsNullOrEmpty(token))
                        {
                            StatusLabel.Text = "SUNUCUYA İLETİLİYOR...";
                            if (currentUserId != 0)
                            {
                                await _signalRService.SendSosAsync(currentUserId, location.Latitude, location.Longitude);

                                if (_localContacts != null && _localContacts.Any())
                                {
                                    foreach (var item in _localContacts)
                                    {
                                        if (item.Id != 0)
                                        {
                                            await _authService.SendSosAlertAsync(currentUserId, item.Id);
                                            Console.WriteLine($"=== 🚀 FÜZE {item.Name} (ID: {item.Id}) İÇİN ATEŞLENDİ! ===");
                                        }
                                    }

                                    Console.WriteLine($"\n\n=== FIREBASE TOKEN (BURAYI KOPYALA) ===\n{token}\n=======================================\n\n");
                                    await Clipboard.Default.SetTextAsync(token);

                                    StatusLabel.Text = "ÇAĞRI YAPILDI!\nKimlik Kopyalandı!";
                                    StatusLabel.TextColor = Colors.White;

                                    if (planBExecuted)
                                    {
                                        MainThread.BeginInvokeOnMainThread(async () => {
                                            await Application.Current.MainPage.DisplayAlert("İNTERNET GELDİ!", "Bağlantı sağlandı. Yardım çağrınız şimdi uygulama üzerinden de (Bildirim/Harita) kişilerinize başarıyla iletildi!", "Tamam");
                                        });
                                    }
                                    else
                                    {
                                        MainThread.BeginInvokeOnMainThread(async () => {
                                            await Application.Current.MainPage.DisplayAlert("Yardım Çağrısı Başarılı", "Hem uygulama içi (SignalR) hem de arka plan (FCM) bildirimleri başarıyla tetiklendi!", "Tamam");
                                        });
                                    }
                                    planASuccessful = true;
                                }
                            }
                            else
                            {
                                StatusLabel.Text = "KONUM VEYA KİMLİK BULUNAMADI!";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusLabel.Text = "HATA OLUŞTU";
                        Console.WriteLine($"SOS Hatası: {ex.Message}");
                    }
                }
                else if (!planASuccessful && !planBExecuted && Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    StatusLabel.Text = "İNTERNET YOK! SMS ATILIYOR...";
                    StatusLabel.TextColor = Colors.Orange;
                    await ExecuteSmsBPlanAsync();
                    planBExecuted = true;
                }
                await Task.Delay(3000);
            }
        }

        private async Task ExecuteSmsBPlanAsync()
        {
            try
            {
                var location = await _locationService.GetCurrentLocationAsync();
                if (location == null) return;

                string lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                string lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
                string mapLink = $"https://www.google.com/maps?q={lat},{lng}";

                string fullName = Preferences.Get("UserFullName", "Bir yakınınız");
                string smsText = $"[ACİL DURUM] {fullName} acil yardım istiyor! Konum: {mapLink}";

                if (_localContacts != null && _localContacts.Any())
                {
                    foreach (var contact in _localContacts)
                    {
                        if (!string.IsNullOrEmpty(contact.PhoneNumber))
                        {
#if ANDROID
                            Console.WriteLine($"\n=== 🚀 HAYALET SMS SİMÜLASYONU ===");
                            Console.WriteLine($"KİME GİDİYOR: {contact.PhoneNumber}");
                            Console.WriteLine($"MESAJ İÇERİĞİ: {smsText}");
                            Console.WriteLine($"===================================\n");
                            var smsManager = Android.Telephony.SmsManager.Default;
                            smsManager.SendTextMessage(contact.PhoneNumber, null, smsText, null, null);
#endif
                        }
                    }
                    StatusLabel.Text = "B PLANI: SMS GÖNDERİLDİ!";
                    StatusLabel.TextColor = Colors.White;

                    MainThread.BeginInvokeOnMainThread(async () => {
                        await Application.Current.MainPage.DisplayAlert("B PLANI DEVREDE", "İnternet bağlantınız olmadığı için konumunuz acil durum kişilerinize SMS olarak iletildi.", "Tamam");
                    });
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "SMS GÖNDERİLEMEDİ!";
                Console.WriteLine($"B Planı Hatası: {ex.Message}");
            }
        }

        private async Task MarkAsSafe()
        {
            bool answer = await DisplayAlert("Güvende misin?", "Acil durum modunu kapatmak istiyor musunuz?", "EVET", "HAYIR");
            if (answer)
            {
                if (currentUserId != 0)
                {
                    await _signalRService.SendSafeAsync(currentUserId);
                }

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

        // --- 🚀 YENİ HARİTA OVERLAY METOTLARI 🚀 ---

        public void OpenOrUpdateEmergencyMap(string victimUserId, string victimName, double lat, double lng)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _activeTrackingUserId = victimUserId;
                _activeTrackingUserName = victimName;

                if (!MapOverlayGrid.IsVisible)
                {
                    MapOverlayGrid.IsVisible = true;
                }

                var targetLocation = new Location(lat, lng);
                MapSpan mapSpan = MapSpan.FromCenterAndRadius(targetLocation, Distance.FromMeters(400));
                EmergencyMap.MoveToRegion(mapSpan);

                EmergencyMap.Pins.Clear();
                var emergencyPin = new Pin
                {
                    Label = $"🚨 {victimName} Yardım İstiyor!",
                    Address = "Canlı Konum",
                    Type = PinType.Place,
                    Location = targetLocation
                };
                EmergencyMap.Pins.Add(emergencyPin);
            });
        }

        private void OnCloseMapClicked(object sender, EventArgs e)
        {
            MapOverlayGrid.IsVisible = false;
            _activeTrackingUserId = null;
        }

        private void OnCall112Clicked(object sender, EventArgs e)
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open("112");
            }
        }

        // SignalR'dan saniye saniye konum geldikçe bu tetiklenecek (ileride eklersin)
        public void OnLocationUpdatedFromSignalR(string updatedUserId, double newLat, double newLng)
        {
            if (MapOverlayGrid.IsVisible && _activeTrackingUserId == updatedUserId)
            {
                OpenOrUpdateEmergencyMap(updatedUserId, _activeTrackingUserName, newLat, newLng);
            }
        }

        // --- SIGNALR DİNLEYİCİLERİ ---

        private void HandleIncomingSos(string senderIdString, string serverSenderName, double lat, double lng)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_isAlertActive) return;

                try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

                bool yardimEt = await DisplayAlert("⚠️ ACİL DURUM!",
                    $"{serverSenderName} yardım istedi!\nOna gitmek istiyor musun?",
                    "GİT (HARİTADA GÖR)", "İPTAL");

                if (yardimEt)
                {
                    // A. Sunucuya haber ver
                    await _signalRService.ConfirmHelp("Bir Dost", senderIdString);

                    // B. DIŞ HARİTA YERİNE ARTIK BİZİM OVERLAY HARİTAYI AÇIYORUZ! 🗺️🔥
                    OpenOrUpdateEmergencyMap(senderIdString, serverSenderName, lat, lng);
                }
            });
        }

        private void HandleHelpConfirmation(string helperName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = $"YARDIM GELİYOR: {helperName}";
                StatusLabel.TextColor = Colors.Green;
            });
        }

        private void HandleIncomingSafe(string senderName)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Arkadaşın güvende olduğunu bildirir, harita açıksa otomatik kapatır
                Application.Current.MainPage.DisplayAlert("✅ DURUM GÜNCELLEMESİ", $"{senderName} şu an güvende olduğunu bildirdi.", "TAMAM");

                if (MapOverlayGrid.IsVisible && _activeTrackingUserName == senderName)
                {
                    MapOverlayGrid.IsVisible = false;
                    _activeTrackingUserId = null;
                }
            });
        }
    }

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