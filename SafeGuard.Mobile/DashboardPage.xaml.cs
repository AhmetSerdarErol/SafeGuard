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
        private IDispatcherTimer _safeWalkTimer;
        private int _safeWalkRemainingSeconds;
        private readonly AuthService _authService = new AuthService();
        private readonly ContactService _contactService = new ContactService();
        private readonly SignalRService _signalRService;
        private readonly LocationService _locationService = new LocationService();
        private readonly EmergencyLocationService _emergencyLocationService = new EmergencyLocationService();
        private int currentUserId;
        private bool isCountingDown = false;
        private bool isCooldown = false;
        private bool isSosActive = false;
        private bool _isAlertActive = false;
        private CancellationTokenSource? _cancelTokenSource;
        private int _selectedTimerSeconds;
        private const double FALL_THRESHOLD = 3.5;
        private string? _activeTrackingUserId = null;
        private string _activeTrackingUserName = "";
        private int _highGForceCounter = 0;

        private ContactModel _selectedContact;

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
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            currentUserId = Preferences.Get("CurrentUserId", 0);

            try
            {
                if (currentUserId != 0)
                {
                    bool realSosStatus = await _authService.CheckMySosStatusAsync(currentUserId);
                    isSosActive = realSosStatus;
                    Preferences.Default.Set("IsSosActiveState", isSosActive);
                }
                else
                {
                    isSosActive = Preferences.Default.Get("IsSosActiveState", false);
                }
            }
            catch
            {
                isSosActive = Preferences.Default.Get("IsSosActiveState", false);
            }

            if (isSosActive)
            {
                SosBtnBorder.BackgroundColor = Colors.Green;
                SosLabel.Text = "GÜVENDEYİM";
                SosLabel.FontSize = 20;
                this.AbortAnimation("PulseEffect");
            }
            else
            {
                SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
                SosLabel.Text = "SOS";
                SosLabel.FontSize = 40;
                StartRedPulse();
            }

            try
            {
#if ANDROID
                var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(SafeGuard.Platforms.Android.FallDetectionService));
                Android.App.Application.Context.StartForegroundService(intent);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ARKA PLAN SERVİSİ HATASI: {ex.Message}");
            }

            FallAlertSystem.OnFallDetected = (gForce) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await TriggerCountdownAsync(gForce);
                });
            };

            LoadSavedTimer();
            string photoUrl = Preferences.Get("UserPhotoUrl", "");
            string fullName = Preferences.Get("UserFullName", "U");

            if (!string.IsNullOrEmpty(photoUrl))
            {
                BottomProfileImage.Source = $"https://wql5wj50-5161.euw.devtunnels.ms/{photoUrl}";
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
                    var contacts = await _authService.GetContactsAsync(currentUserId);
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
            if (isSosActive)
            {
                await MarkAsSafe();
                StopBackgroundService();
                return;
            }

            if (isCooldown) return;

            if (isCountingDown)
            {
                CancelSosProcess();
                StopBackgroundService();
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

            SosLabel.Text = "İPTAL";
            SosLabel.FontSize = 30;
            SosBtnBorder.BackgroundColor = Colors.Gray;

            int totalWaitMs = Preferences.Get("EmergencyTimerSeconds", 5) * 1000;

            try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                while (stopwatch.ElapsedMilliseconds < totalWaitMs)
                {
                    if (token.IsCancellationRequested) return;

                    SosProgress.Progress = (double)stopwatch.ElapsedMilliseconds / totalWaitMs;

                    int remainingSeconds = (totalWaitMs - (int)stopwatch.ElapsedMilliseconds) / 1000;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"İPTAL ETMEK İÇİN DOKUN ({remainingSeconds}s)";
                        StatusLabel.TextColor = Colors.Orange;
                    });

                    await Task.Delay(15);
                }

                if (!token.IsCancellationRequested)
                {
                    SosProgress.Progress = 1;
                    StartBackgroundService();
                    await TriggerSos();
                }
            }
            catch (Exception ex)
            {
                CancelSosProcess();
                await DisplayAlert("Geri Sayım Hatası", ex.Message, "Tamam");
            }
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

            Preferences.Default.Set("IsSosActiveState", true);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    StartGreenPulse();
                    SosBtnBorder.BackgroundColor = Colors.Green;
                    SosLabel.Text = "GÜVENDEYİM";
                    SosLabel.FontSize = 20;
                }
                catch { }
            });

            _ = Task.Run(async () =>
            {
                try { await StartBlackBoxRecordingAsync(); }
                catch (Exception ex) { Console.WriteLine($"Ses kaydı hatası: {ex.Message}"); }
            });

            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            while (isSosActive)
            {
                if (!planASuccessful && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        MainThread.BeginInvokeOnMainThread(() => { if (isSosActive) { try { StatusLabel.Text = "KONUM VE KİMLİK ALINIYOR..."; } catch { } } });

                        await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                        var token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                        Location location = null;
                        try
                        {
                            location = await _locationService.GetCurrentLocationAsync().WaitAsync(TimeSpan.FromSeconds(4));
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("GPS 4 saniyede yanıt vermedi son bilinen konuma dönülüyor...");
                            location = await Geolocation.Default.GetLastKnownLocationAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Konum alınırken kritik hata: {ex.Message}");
                            location = await Geolocation.Default.GetLastKnownLocationAsync();
                        }

                        if (!isSosActive) return;

                        if (location == null)
                        {
                            location = new Location(0, 0);
                        }

                        if (location != null && !string.IsNullOrEmpty(token))
                        {
                            MainThread.BeginInvokeOnMainThread(() => { if (isSosActive) { try { StatusLabel.Text = "SUNUCUYA İLETİLİYOR..."; } catch { } } });

                            if (currentUserId != 0)
                            {
                                bool fuzeGittimi = await _authService.SendSosAlertAsync(currentUserId, location.Latitude, location.Longitude);

                                if (!isSosActive) return;

                                if (!fuzeGittimi)
                                {
                                    MainThread.BeginInvokeOnMainThread(async () =>
                                    {
                                        if (!isSosActive) return;
                                        try
                                        {
                                            StatusLabel.Text = "API REDDETTİ!";
                                            StatusLabel.TextColor = Colors.Red;
                                            await Application.Current.MainPage.DisplayAlert("Sunucu Hatası!", "Füze yola çıktı ama API kapıdan çevirdi!", "Tamam");
                                        }
                                        catch { }
                                    });

                                    isSosActive = false;
                                    Preferences.Default.Set("IsSosActiveState", false);
                                    return;
                                }

                                Console.WriteLine($"=== 🚀 FÜZE KOORDİNATLARLA ATEŞLENDİ Enlem: {location.Latitude} Boylam: {location.Longitude} ===");

                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _signalRService.SendSosAsync(currentUserId, location.Latitude, location.Longitude);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"SignalR Hatası Arka Plan: {ex.Message}");
                                    }
                                });

                                _ = _emergencyLocationService.StartBroadcastingAsync(currentUserId.ToString());

                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    if (!isSosActive) return;
                                    try
                                    {
                                        await Clipboard.Default.SetTextAsync(token);
                                        StatusLabel.Text = "ÇAĞRI YAPILDI!\nKimlik Kopyalandı!";
                                        StatusLabel.TextColor = Colors.White;
                                    }
                                    catch { }
                                });

                                if (planBExecuted)
                                {
                                    MainThread.BeginInvokeOnMainThread(async () =>
                                    {
                                        if (!isSosActive) return;
                                        try { await Application.Current.MainPage.DisplayAlert("İNTERNET GELDİ!", "Bağlantı sağlandı. Yardım çağrınız şimdi uygulama üzerinden de iletildi!", "Tamam"); } catch { }
                                    });
                                }
                                else
                                {
                                    // İSTENEN YENİ VE PROFESYONEL ONAY MESAJI
                                    MainThread.BeginInvokeOnMainThread(async () =>
                                    {
                                        if (!isSosActive) return;
                                        try { await Application.Current.MainPage.DisplayAlert("Yardım Çağrısı İletildi", "Yardım çağrınız yakınlarınıza başarıyla gönderildi. En kısa sürede yardım gelecek, lütfen sakin olunuz.", "Tamam"); } catch { }
                                    });
                                }

                                planASuccessful = true;
                            }
                            else
                            {
                                MainThread.BeginInvokeOnMainThread(() => { if (isSosActive) { try { StatusLabel.Text = "KULLANICI KİMLİĞİ BULUNAMADI!"; } catch { } } });
                            }
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() => { if (isSosActive) { try { StatusLabel.Text = "KONUM VEYA KİMLİK BULUNAMADI!"; } catch { } } });
                        }
                    }
                    catch (Exception ex)
                    {
                        MainThread.BeginInvokeOnMainThread(() => { if (isSosActive) { try { StatusLabel.Text = "HATA OLUŞTU"; } catch { } } });
                        Console.WriteLine($"SOS Hatası: {ex.Message}");
                    }
                }
                else if (!planASuccessful && !planBExecuted && Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!isSosActive) return;
                        try
                        {
                            StatusLabel.Text = "İNTERNET YOK! SMS ATILIYOR...";
                            StatusLabel.TextColor = Colors.Orange;
                        }
                        catch { }
                    });
                    await ExecuteSmsBPlanAsync();
                    planBExecuted = true;
                }
                await Task.Delay(3000);
            }
        }

        private async void OnDeleteContactClicked(object sender, EventArgs e)
        {
            if (_selectedContact == null) return;

            bool eminMisiniz = await DisplayAlert("Kişiyi Sil", $"{_selectedContact.Name} isimli kişiyi acil durum yakınlarınızdan çıkarmak istediğinize emin misiniz?", "Evet, Sil", "İptal");

            if (eminMisiniz)
            {
                try
                {
                    bool isDeleted = await _contactService.DeleteContactAsync(_selectedContact.Id);

                    if (isDeleted)
                    {
                        await DisplayAlert("Başarılı", $"{_selectedContact.Name} yakınlarınızdan çıkarıldı.", "Tamam");

                        if (_localContacts != null && _localContacts.Contains(_selectedContact))
                        {
                            _localContacts.Remove(_selectedContact);
                        }

                        BottomSheetGrid.IsVisible = false;
                        await LoadContacts();
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Kişi silinemedi. Sunucu kaynaklı bir sorun olabilir.", "Tamam");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", "Silme işlemi başarısız oldu: " + ex.Message, "Tamam");
                }
            }
        }

        private async void StartBackgroundService()
        {
            try
            {
                var locStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (locStatus != PermissionStatus.Granted)
                {
                    locStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (locStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Uyarı", "Konum izni verilmeden arka plan takibi yapılamaz.", "Tamam");
                        return;
                    }
                }

                if (DeviceInfo.Version.Major >= 13)
                {
                    var notifStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (notifStatus != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.PostNotifications>();
                    }
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Geliştirici test uyarısı tamamen silindi.
                });
#if ANDROID
                var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(SafeGuard.Platforms.Android.ForegroundLocationService));
                Android.App.Application.Context.StartForegroundService(intent);
#endif
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisplayAlert("Servis Hatası", ex.Message, "Tamam");
                });
            }
        }

        private void StopBackgroundService()
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(SafeGuard.Platforms.Android.ForegroundLocationService));
            Android.App.Application.Context.StopService(intent);
#endif
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
                    StatusLabel.Text = "ÇEVRİMDİŞİ SİNYAL İLETİLDİ";
                    StatusLabel.TextColor = Colors.White;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Çevrimdışı Sinyal İletildi",
                            "İnternet bağlantısı kurulamadı. Endişelenmeyin, sistem otomatik olarak güncel konumunuzu acil durum kişilerinize SMS üzerinden başarıyla gönderdi.",
                            "Tamam");
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
                isSosActive = false;
                isCooldown = false;
                ResetScreen();

                await _emergencyLocationService.StopBroadcastingAsync();

                if (currentUserId != 0)
                {
                    await _authService.CancelSosAsync(currentUserId);

                    _ = Task.Run(async () =>
                    {
                        try { await _signalRService.SendSafeAsync(currentUserId); }
                        catch { }
                    });
                }
            }
        }

        private void ResetScreen()
        {
            isSosActive = false;
            isCountingDown = false;
            isCooldown = false;

            Preferences.Default.Set("IsSosActiveState", false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                this.AbortAnimation("SuccessEffect");
                this.AbortAnimation("PulseEffect");
                SosProgress.Progress = 0;

                SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
                SosLabel.Text = "SOS";
                SosLabel.FontSize = 40;

                StatusLabel.Text = "YARDIM ÇAĞIRMAK İÇİN DOKUN";
                StatusLabel.TextColor = Colors.Gray;

                StartRedPulse();
            });
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


        public static void OpenOrUpdateEmergencyMap(string victimUserId, string victimName, double lat, double lng)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var nav = Application.Current.MainPage.Navigation;
                    var currentPage = nav.ModalStack.LastOrDefault();
                    if (currentPage is SafeGuard.Mobile.Views.LiveTrackingPage)
                    {
                        return;
                    }

                    var liveTrackingPage = new SafeGuard.Mobile.Views.LiveTrackingPage(victimUserId, lat, lng);

                    await nav.PushModalAsync(liveTrackingPage, true);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Harita Hatası", "Harita açılamadı: " + ex.Message, "Tamam");
                }
            });
        }


        public void OnLocationUpdatedFromSignalR(string updatedUserId, double newLat, double newLng)
        {
        }



        private void HandleIncomingSos(string senderIdString, string serverSenderName, double lat, double lng)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {

                bool yardimEt = await Application.Current.MainPage.DisplayAlert("⚠️ ACİL DURUM!",
                    $"{serverSenderName} yardım istedi!\nOna gitmek istiyor musun?",
                    "GİT (HARİTADA GÖR)", "İPTAL");

                if (yardimEt)
                {

                    await _signalRService.ConfirmHelp("Bir Dost", senderIdString);


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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                isSosActive = false;
                isCooldown = false;
                Preferences.Default.Set("IsSosActiveState", false);

                ResetScreen();

                await Application.Current.MainPage.DisplayAlert("Bilgi", "Durum çözüldü, sistem normale döndü.", "TAMAM");

                var currentPage = Application.Current.MainPage.Navigation.ModalStack.LastOrDefault();
                if (currentPage is LiveTrackingPage)
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
            });
        }

        private void OnContactTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ContactModel contact)
            {
                _selectedContact = contact;
                SheetPermissionSwitch.Toggled -= OnSheetPermissionToggled;
                SheetPermissionSwitch.IsToggled = contact.IAllowedThem;
                SheetPermissionSwitch.Toggled += OnSheetPermissionToggled;
                SheetNameLabel.Text = contact.Name;

                if (contact.IsSosActive)
                {
                    SheetStatusLabel.Text = "🚨 DURUM: ACİL YARDIM BEKLİYOR!";
                    SheetStatusLabel.TextColor = Colors.Red;
                    TrackLocationBtn.IsVisible = true;
                }
                else
                {
                    SheetStatusLabel.Text = "✅ DURUM: GÜVENDE";
                    SheetStatusLabel.TextColor = Colors.LightGreen;
                    TrackLocationBtn.IsVisible = false;
                }

                SheetDateLabel.Text = contact.LastStatusUpdate.HasValue
                    ? $"Son Güncelleme: {contact.LastStatusUpdate.Value:dd.MM.yyyy HH:mm}"
                    : "Son Güncelleme: Bilinmiyor";

                BottomSheetGrid.IsVisible = true;
            }
        }

        private void OnCloseBottomSheetTapped(object sender, TappedEventArgs e)
        {
            BottomSheetGrid.IsVisible = false;
        }

        private async void OnTrackLocationClicked(object sender, EventArgs e)
        {
            if (_selectedContact != null)
            {
                BottomSheetGrid.IsVisible = false;

                var myLocation = await _locationService.GetCurrentLocationAsync();

                if (myLocation != null)
                {
                    OpenOrUpdateEmergencyMap(
                        _selectedContact.Id.ToString(),
                        _selectedContact.Name,
                        _selectedContact.Latitude,
                        _selectedContact.Longitude
                    );
                }
            }
        }

        private async void OnShowMedicalIdClicked(object sender, EventArgs e)
        {
            try
            {
                if (_selectedContact != null)
                {
                    if (_selectedContact.TheyAllowedMe == true || _selectedContact.IsSosActive == true)
                    {
                        await Navigation.PushModalAsync(new MedicalIdPage(_selectedContact));
                    }
                    else
                    {
                        await DisplayAlert("Erişim Gizli", "Şu an acil bir durum bulunmuyor. Bu kişinin tıbbi kimlik bilgilerini sadece acil durumlarda veya size özel izin verildiğinde görebilirsiniz.", "Anladım");
                    }
                }
                else
                {
                    await DisplayAlert("Uyarı", "Kişi verisi alınamadı!", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Çökme Sebebi", ex.Message, "Tamam");
            }
        }

        private async void OnSheetPermissionToggled(object sender, ToggledEventArgs e)
        {
            if (_selectedContact == null) return;

            try
            {
                int currentUserId = Preferences.Get("CurrentUserId", 0);
                int contactId = _selectedContact.Id;
                bool isAllowed = e.Value;

                string token = Preferences.Get("Token", "");

                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                    string apiUrl = $"https://wql5wj50-7209.euw.devtunnels.ms/api/helpers/UpdateMedicalIdPermission?currentUserId={currentUserId}&contactId={contactId}&isAllowed={isAllowed}";

                    var response = await client.PostAsync(apiUrl, null);

                    if (response.IsSuccessStatusCode)
                    {
                        _selectedContact.IAllowedThem = isAllowed;
                        Console.WriteLine("[SAFEGUARD] İzin başarıyla güncellendi!");
                    }
                    else
                    {
                        SheetPermissionSwitch.IsToggled = !isAllowed;
                        await DisplayAlert("Hata", $"API Reddedildi. Durum Kodu: {(int)response.StatusCode}", "Tamam");
                    }
                }
            }
            catch (Exception ex)
            {
                SheetPermissionSwitch.IsToggled = !e.Value;
                await DisplayAlert("Bağlantı Hatası", $"Sunucuya ulaşılamadı: {ex.Message}", "Tamam");
            }
        }

        private async Task StartBlackBoxRecordingAsync()
        {
            try
            {
                var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var s = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                    if (s != PermissionStatus.Granted)
                    {
                        s = await Permissions.RequestAsync<Permissions.Microphone>();
                    }
                    return s;
                });

                if (status != PermissionStatus.Granted)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        Application.Current.MainPage.DisplayAlert("HATA", "Mikrofon izni reddedildi!", "Tamam"));
                    return;
                }

                var audioRecorder = Plugin.Maui.Audio.AudioManager.Current.CreateRecorder();
                await audioRecorder.StartAsync();

                await Task.Delay(15000);

                var recordedAudio = await audioRecorder.StopAsync();
                var filePath = Path.Combine(FileSystem.CacheDirectory, $"BlackBox_SOS_{DateTime.Now:yyyyMMdd_HHmmss}.m4a");

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    using (var audioStream = recordedAudio.GetAudioStream())
                    {
                        await audioStream.CopyToAsync(fileStream);
                    }
                }

                await UploadAudioToApiAsync(filePath);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    Application.Current.MainPage.DisplayAlert("❌ SİSTEM ÇÖKTÜ", $"Hata detayı: {ex.Message}", "Tamam"));
            }
        }

        private void OnOpenTimerSettingsTapped(object sender, TappedEventArgs e)
        {
            LoadSavedTimer();
            TimerBottomSheetGrid.IsVisible = true;
        }

        private void OnCloseTimerSettingsTapped(object sender, TappedEventArgs e)
        {
            TimerBottomSheetGrid.IsVisible = false;
        }

        private void OnTimerSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            int minutes = (int)Math.Round(MinuteSlider.Value);
            int seconds = (int)Math.Round(SecondSlider.Value);
            _safeWalkRemainingSeconds = (minutes * 60) + seconds;

            MinuteLabel.Text = $"{minutes} Dakika";
            SecondLabel.Text = $"{seconds} Saniye";
            TotalTimeLabel.Text = $"Toplam: {minutes} Dakika {seconds} Saniye";
        }

        private void OnStartSafeWalkClicked(object sender, EventArgs e)
        {
            int minutes = (int)Math.Round(MinuteSlider.Value);
            int seconds = (int)Math.Round(SecondSlider.Value);
            _safeWalkRemainingSeconds = (minutes * 60) + seconds;

            if (_safeWalkRemainingSeconds <= 0)
            {
                DisplayAlert("Uyarı", "Lütfen 0'dan büyük bir süre seçin.", "Tamam");
                return;
            }

            Preferences.Set("SafeWalkTimerSeconds", _safeWalkRemainingSeconds);

            TimerBottomSheetGrid.IsVisible = false;
            SafeWalkActiveBanner.IsVisible = true;
            UpdateSafeWalkLabel();

            if (_safeWalkTimer != null && _safeWalkTimer.IsRunning)
                _safeWalkTimer.Stop();

            _safeWalkTimer = Application.Current.Dispatcher.CreateTimer();
            _safeWalkTimer.Interval = TimeSpan.FromSeconds(1);
            _safeWalkTimer.Tick += async (s, ev) =>
            {
                _safeWalkRemainingSeconds--;
                UpdateSafeWalkLabel();

                if (_safeWalkRemainingSeconds <= 0)
                {
                    _safeWalkTimer.Stop();
                    SafeWalkActiveBanner.IsVisible = false;

                    StartBackgroundService();
                    await TriggerSos();
                }
            };
            _safeWalkTimer.Start();
        }

        private void OnCancelSafeWalkClicked(object sender, EventArgs e)
        {
            if (_safeWalkTimer != null && _safeWalkTimer.IsRunning)
            {
                _safeWalkTimer.Stop();
            }

            SafeWalkActiveBanner.IsVisible = false;
        }

        private void UpdateSafeWalkLabel()
        {
            TimeSpan time = TimeSpan.FromSeconds(_safeWalkRemainingSeconds);
            SafeWalkCountdownLabel.Text = $"Kalan Süre: {time.ToString(@"mm\:ss")}";
        }

        private void LoadSavedTimer()
        {
            int savedSeconds = Preferences.Get("SafeWalkTimerSeconds", 300);
            MinuteSlider.Value = savedSeconds / 60;
            SecondSlider.Value = savedSeconds % 60;
        }
        private void OnFakeCallTriggered(object sender, EventArgs e)
        {
            FakeCallBtn.IsEnabled = false;
            FakeCallBtn.Text = "Hazırlanıyor...";
            var animation = new Animation(v => FakeCallFillBar.WidthRequest = v, 0, 200);

            animation.Commit(this, "FillAnimation", length: 5000, easing: Easing.Linear, finished: async (v, c) =>
            {
                FakeCallFillBar.WidthRequest = 0;
                FakeCallBtn.Text = "📞 Sahte Arama Başlat";
                FakeCallBtn.IsEnabled = true;

                await Navigation.PushModalAsync(new SafeGuard.Mobile.Views.FakeCallPage());
            });
        }
        private bool _isAlertCanceled = false;

        private async Task TriggerCountdownAsync(string gForce)
        {
            _isAlertCanceled = false;

            Vibration.Default.Vibrate(TimeSpan.FromSeconds(10));
            _ = Task.Run(async () =>
            {
                await Task.Delay(30000);
                if (!_isAlertCanceled)
                {
                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromSeconds(2));

                        await TriggerSos();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Arka plan SOS fırlatma hatası: {ex.Message}");
                    }
                }
            });

            try
            {
                bool result = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Application.Current.MainPage.DisplayAlert(
                        "💥 ÇARPMA ALGILANDI!",
                        $"{gForce} G şiddetinde çarpma tespit edildi!\n\n30 Saniye içinde otomatik SOS gönderilecek!",
                        "İYİYİM (İPTAL ET)",
                        "HEMEN YARDIM ÇAĞIR");
                });

                if (result)
                {
                    _isAlertCanceled = true;
                    Vibration.Default.Cancel();

                    await MainThread.InvokeOnMainThreadAsync(async () => {
                        await Application.Current.MainPage.DisplayAlert("İptal Edildi", "Geçmiş olsun, sistem normale döndü.", "Tamam");
                    });
                }
                else
                {
                    _isAlertCanceled = true;
                    Vibration.Default.Cancel();
                    await TriggerSos();
                }
            }
            catch (Exception)
            {
            }
        }

        private void StartFallDetection()
        {
            try
            {
                if (DeviceInfo.DeviceType != DeviceType.Virtual)
                {
                    if (Accelerometer.Default.IsSupported && !Accelerometer.Default.IsMonitoring)
                    {
                        Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                        Accelerometer.Default.Start(SensorSpeed.UI);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            double gForce = Math.Sqrt(data.Acceleration.X * data.Acceleration.X +
                                      data.Acceleration.Y * data.Acceleration.Y +
                                      data.Acceleration.Z * data.Acceleration.Z);

            if (gForce > FALL_THRESHOLD)
            {
                _highGForceCounter++;

                if (_highGForceCounter >= 3)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                    _highGForceCounter = 0;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await TriggerCountdownAsync(gForce.ToString("F2"));
                    });
                }
            }
            else
            {
                _highGForceCounter = 0;
            }
        }

        private async Task UploadAudioToApiAsync(string filePath)
        {
            try
            {
                int currentUserId = Preferences.Get("CurrentUserId", 0);
                string token = Preferences.Get("Token", "");

                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var content = new MultipartFormDataContent())
                    {
                        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        var streamContent = new StreamContent(fileStream);
                        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mp4");

                        content.Add(streamContent, "audioFile", Path.GetFileName(filePath));
                        content.Add(new StringContent(currentUserId.ToString()), "userId");

                        string apiUrl = "https://wql5wj50-7209.euw.devtunnels.ms/api/sos/UploadAudio";

                        var response = await client.PostAsync(apiUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            File.Delete(filePath);
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                                Application.Current.MainPage.DisplayAlert("Hata", $"API Dosyayı Reddetti: {response.StatusCode}", "Tamam"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    Application.Current.MainPage.DisplayAlert("Gönderim Hatası", ex.Message, "Tamam"));
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
    public static class FallAlertSystem
    {
        public static Action<string> OnFallDetected;
    }
}