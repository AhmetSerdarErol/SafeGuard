using System.Diagnostics;
using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;
using Contact = SafeGuard.Mobile.Models.Contact;

namespace SafeGuard.Mobile
{
    public partial class DashboardPage : ContentPage
    {
        private bool isCountingDown = false;
        private bool isCooldown = false;
        private CancellationTokenSource? _cancelTokenSource;

        private readonly AuthService _authService = new AuthService();
        private readonly ContactService _contactService = new ContactService();

        public DashboardPage()
        {
            InitializeComponent();

            // İlk açılışta ismi yaz (Garanti olsun diye hem buraya hem OnAppearing'e koyduk)
            UpdateWelcomeMessage();
            StartRedPulse();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Sayfa her göründüğünde ismi ve listeyi güncelle
            UpdateWelcomeMessage();
            await LoadContacts();
        }

        // --- İSMİ YAZDIRAN FONKSİYON ---
        private void UpdateWelcomeMessage()
        {
            string savedName = Preferences.Get("UserFullName", "");
            if (!string.IsNullOrEmpty(savedName))
            {
                WelcomeLabel.Text = $"Hoş Geldiniz, {savedName}";
            }
            else
            {
                WelcomeLabel.Text = "Hoş Geldiniz";
            }
        }

        private async Task LoadContacts()
        {
            try
            {
                // Hafızadaki "+" butonunu koruma mantığı
                if (ContactsList.Children.Count > 0)
                {
                    // XAML'daki en baştaki "+" butonunu (ilk elemanı) sakla
                    var plusButton = ContactsList.Children[0];
                    ContactsList.Children.Clear();
                    ContactsList.Children.Add(plusButton); // Geri ekle
                }

                // ID'yi al ve servise git
                int userId = Preferences.Get("CurrentUserId", 0);
                if (userId != 0)
                {
                    var contacts = await _contactService.GetContactsAsync(userId);
                    foreach (var contact in contacts)
                    {
                        AddContactToUI(contact);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Liste Hatası: {ex.Message}");
            }
        }

        private async void OnAddContactTapped(object sender, EventArgs e)
        {
            int userId = Preferences.Get("CurrentUserId", 0);
            if (userId == 0)
            {
                await DisplayAlert("Hata", "Oturum süresi dolmuş. Lütfen tekrar giriş yapın.", "Tamam");
                return;
            }

            string name = await DisplayPromptAsync("Kişi Ekle", "Yakınınızın adını girin:", "İleri", "İptal");
            if (string.IsNullOrWhiteSpace(name)) return;

            string phone = await DisplayPromptAsync("Kişi Ekle", $"{name} için telefon numarası:", "Kaydet", "İptal", keyboard: Keyboard.Telephone);
            if (string.IsNullOrWhiteSpace(phone)) return;

            // UserId ile kayıt yapıyoruz
            var newContact = new Contact
            {
                Name = name,
                PhoneNumber = phone,
                UserId = userId
            };

            var addedContact = await _contactService.AddContactAsync(newContact);

            if (addedContact != null)
            {
                AddContactToUI(addedContact);
                await DisplayAlert("Başarılı", $"{name} listeye eklendi.", "Tamam");
            }
            else
            {
                await DisplayAlert("Hata", "Kişi kaydedilemedi. Sunucu bağlantısını kontrol edin.", "Tamam");
            }
        }

        private void AddContactToUI(Contact contact)
        {
            string initials = contact.Name.Length >= 2 ? contact.Name.Substring(0, 2).ToUpper() : contact.Name.Substring(0, 1).ToUpper();

            Color statusColor = contact.VerificationStatus switch
            {
                "Verified" => Color.FromArgb("#4CAF50"),
                "Blocked" => Color.FromArgb("#D32F2F"),
                _ => Color.FromArgb("#FBC02D")
            };

            var stack = new VerticalStackLayout { Spacing = 5 };

            var border = new Border
            {
                HeightRequest = 65,
                WidthRequest = 65,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 32.5 },
                Stroke = Colors.Transparent,
                BackgroundColor = statusColor,
                Padding = 0
            };

            var label = new Label
            {
                Text = initials,
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            border.Content = label;

            var nameLabel = new Label
            {
                Text = contact.Name,
                TextColor = Colors.White,
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaximumWidthRequest = 70
            };

            stack.Children.Add(border);
            stack.Children.Add(nameLabel);
            ContactsList.Children.Add(stack);
        }

        // --- SOS ve ANİMASYONLAR (Orijinal Kodların) ---
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

        private async void OnSosTapped(object sender, EventArgs e)
        {
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
            StatusLabel.Text = "GÖNDERİLİYOR...\nDURDURMAK İÇİN DOKUN!"; StatusLabel.TextColor = Colors.Orange;
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
                if (!token.IsCancellationRequested) { SosProgress.Progress = 1; TriggerSos(); }
            }
            catch { CancelSosProcess(); }
        }

        private void CancelSosProcess()
        {
            if (isCooldown) return;
            if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested) _cancelTokenSource.Cancel();
            isCountingDown = false;
            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(SosProgress);
            SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS"; SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM İÇİN DOKUN"; StatusLabel.TextColor = Color.FromArgb("#D32F2F");
        }

        private async void TriggerSos()
        {
            isCountingDown = false; isCooldown = true; StartGreenPulse();
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            bool success = await _authService.SendSosAlertAsync(41.0082, 28.9784);

            SosBtnBorder.BackgroundColor = Colors.Green; SosLabel.Text = "OK";
            StatusLabel.Text = success ? "YARDIM GÖNDERİLDİ!" : "HATA!";
            StatusLabel.TextColor = success ? Colors.Green : Colors.Orange;
            await DisplayAlert("Bilgi", success ? "Yardım çağrısı iletildi." : "Bağlantı hatası.", "TAMAM");

            await Task.Delay(5000);
            isCooldown = false; ResetScreen();
        }

        private void ResetScreen()
        {
            this.AbortAnimation("SuccessEffect"); StartRedPulse();
            isCountingDown = false; SosProgress.Progress = 0;
            SosBtnBorder.BackgroundColor = Color.FromArgb("#D32F2F");
            SosLabel.Text = "SOS"; SosLabel.FontSize = 40;
            StatusLabel.Text = "YARDIM İÇİN 3 SN BASILI TUT"; StatusLabel.TextColor = Color.FromArgb("#D32F2F");
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("CurrentUserId");
            Preferences.Remove("UserFullName");
            Preferences.Remove("auth_token");
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
}