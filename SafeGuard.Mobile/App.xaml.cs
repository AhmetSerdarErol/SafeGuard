using Plugin.Firebase.CloudMessaging;

namespace SafeGuard.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // 1. ONBOARDING (TANITIM) KONTROLÜ
            bool isOnboardingComplete = Preferences.Get("OnboardingComplete", false);

            if (!isOnboardingComplete)
            {
                // Tanıtımı izlemediyse Onboarding sayfasını aç
                MainPage = new NavigationPage(new OnboardingPage());
            }
            else
            {
                // 2. GİRİŞ YAPMIŞ MI KONTROLÜ
                if (Preferences.ContainsKey("CurrentUserId"))
                {
                    MainPage = new NavigationPage(new DashboardPage());
                }
                else
                {
                    MainPage = new NavigationPage(new MainPage()); // Login Sayfası
                }
            }

            // 🚨 FİREBASE BİLDİRİM DİNLEYİCİSİ
            CrossFirebaseCloudMessaging.Current.NotificationReceived += (s, e) =>
            {
#if ANDROID
                System.Diagnostics.Debug.WriteLine("🚨 FÜZE TESPİT EDİLDİ! BİLDİRİM GELDİ! 🚨");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var context = Android.App.Application.Context;
                    var intent = new Android.Content.Intent(context, typeof(Platforms.Android.EmergencyActivity));
                    intent.AddFlags(Android.Content.ActivityFlags.NewTask |
                                    Android.Content.ActivityFlags.ClearTop |
                                    Android.Content.ActivityFlags.SingleTop);

                    intent.PutExtra("senderName", "Acil Durum!");

                    context.StartActivity(intent);
                });
#endif
            };
        }
    }
}