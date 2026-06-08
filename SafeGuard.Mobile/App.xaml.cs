using System.Globalization;
using Plugin.Firebase.CloudMessaging;
using SafeGuard.Mobile.Views;

namespace SafeGuard.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Task.Run(async () =>
            {
                try
                {
                    var gercekToken = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                    System.Diagnostics.Debug.WriteLine("=========================================");
                    System.Diagnostics.Debug.WriteLine("🚨 FIREBASE'DEN ZORLA ALINAN GERÇEK TOKEN: " + gercekToken);
                    System.Diagnostics.Debug.WriteLine("=========================================");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("🚨 TOKEN ALINIRKEN HATA ÇIKTI: " + ex.Message);
                }
            });

            CrossFirebaseCloudMessaging.Current.NotificationTapped += (sender, e) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var data = e.Notification.Data;

                    if (data.ContainsKey("latitude") && data.ContainsKey("longitude"))
                    {
                        string vName = data.ContainsKey("userName") ? data["userName"].ToString() : "Yardım İsteyen";
                        string vId = data.ContainsKey("userId") ? data["userId"].ToString() : "0";

                        double lat = Convert.ToDouble(data["latitude"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                        double lng = Convert.ToDouble(data["longitude"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);

                        var liveTrackingPage = new SafeGuard.Mobile.Views.LiveTrackingPage(vId, lat, lng);

                        if (Application.Current != null && Application.Current.MainPage != null)
                        {
                            await Application.Current.MainPage.Navigation.PushModalAsync(liveTrackingPage, true);
                        }
                    }
                });
            };

            bool isOnboardingComplete = Preferences.Get("OnboardingComplete", false);

            if (!isOnboardingComplete)
            {
                MainPage = new NavigationPage(new OnboardingPage());
            }
            else
            {
                if (Preferences.ContainsKey("CurrentUserId"))
                {
                    MainPage = new NavigationPage(new DashboardPage());
                }
                else
                {
                    MainPage = new NavigationPage(new MainPage()); 
                }
            }
        }
    }
}