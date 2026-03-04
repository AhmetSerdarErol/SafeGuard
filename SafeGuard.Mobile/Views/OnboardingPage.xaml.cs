using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;

namespace SafeGuard.Mobile
{
    public partial class OnboardingPage : ContentPage
    {
        public ObservableCollection<OnboardingItem> OnboardingSteps { get; set; }

        private bool _isProcessing = false;

        public OnboardingPage()
        {
            InitializeComponent();

            OnboardingSteps = new ObservableCollection<OnboardingItem>
            {
                new OnboardingItem { Title = "SafeGuard'a Hoş Geldiniz", Description = "Acil durumlarda sizin ve sevdiklerinizin en hızlı kurtarıcısı.", ImageName = "koruma.png" },
                new OnboardingItem { Title = "Temel Erişimler", Description = "Konumunuzu belirlemek ve acil SMS gönderebilmek için ekrandaki uyarılara izin verin.", ImageName = "konum.png" },
                new OnboardingItem { Title = "Arka Plan Çalışması", Description = "Uygulamanın uyku moduna girmemesi için Pil Optimizasyonunu kapatmalısınız.", ImageName = "pil.png" },
                new OnboardingItem { Title = "Acil Durum Ekranı", Description = "Telefon kilitliyken bile kırmızı alarmı görebilmek için 'Diğer uygulamaların üzerinde göster' izni gereklidir.", ImageName = "app_logo.jpg" },
                new OnboardingItem { Title = "Her Şey Hazır!", Description = "Sisteminiz başarıyla kuruldu. Artık güvendesiniz.", ImageName = "koruma.png" }
            };

            BindingContext = this;
            OnboardingCarousel.ItemsSource = OnboardingSteps;
            OnboardingCarousel.PositionChanged += OnboardingCarousel_PositionChanged;
        }

        private void OnboardingCarousel_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (e.CurrentPosition == 0) NextButton.Text = "Başla";
            else if (e.CurrentPosition == 1) NextButton.Text = "İzinleri İste";
            else if (e.CurrentPosition == 2) NextButton.Text = "Pil Ayarlarına Git";
            else if (e.CurrentPosition == 3) NextButton.Text = "Ekran Ayarlarına Git";
            else if (e.CurrentPosition == 4) NextButton.Text = "Uygulamaya Geç";
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                int currentIndex = OnboardingCarousel.Position;

                if (currentIndex == 0)
                {
                    // ÇÖKERTEN 'Position = 1' YERİNE GÜVENLİ KAYDIRMA KODU
                    OnboardingCarousel.ScrollTo(1, position: ScrollToPosition.Center, animate: true);
                }
                else if (currentIndex == 1)
                {
                    try
                    {
                        var locStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                        if (locStatus != PermissionStatus.Granted) await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                        var smsStatus = await Permissions.CheckStatusAsync<Permissions.Sms>();
                        if (smsStatus != PermissionStatus.Granted) await Permissions.RequestAsync<Permissions.Sms>();
                    }
                    catch { }

                    OnboardingCarousel.ScrollTo(2, position: ScrollToPosition.Center, animate: true);
                }
                else if (currentIndex == 2)
                {
#if ANDROID
                    try
                    {
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.StartActivity(intent);
                    }
                    catch { }
#endif
                    OnboardingCarousel.ScrollTo(3, position: ScrollToPosition.Center, animate: true);
                }
                else if (currentIndex == 3)
                {
#if ANDROID
                    try
                    {
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageOverlayPermission);
                        intent.SetData(Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName));
                        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.StartActivity(intent);
                    }
                    catch { }
#endif
                    OnboardingCarousel.ScrollTo(4, position: ScrollToPosition.Center, animate: true);
                }
                else if (currentIndex == 4)
                {
                    Preferences.Set("OnboardingComplete", true);
                    Page targetPage = Preferences.ContainsKey("CurrentUserId") ? new DashboardPage() : new MainPage();
                    await Navigation.PushAsync(targetPage);
                    Navigation.RemovePage(this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GENEL HATA: {ex.Message}");
            }
            finally
            {
                
                await Task.Delay(500);
                _isProcessing = false;
            }
        }
    }

    public class OnboardingItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageName { get; set; }
    }
}