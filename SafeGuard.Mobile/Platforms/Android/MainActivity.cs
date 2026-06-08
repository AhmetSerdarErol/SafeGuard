using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SafeGuard.Mobile;
using System.Globalization;

namespace SafeGuard.Mobile.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            CheckIntentForMap(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            CheckIntentForMap(intent);
        }

        private void CheckIntentForMap(Intent intent)
        {
            if (intent?.GetBooleanExtra("openMap", false) == true)
            {
                string targetUserId = intent.GetStringExtra("targetUserId") ?? "";
                string targetUserName = intent.GetStringExtra("targetUserName") ?? "";
                string latStr = intent.GetStringExtra("latitude") ?? "0";
                string lngStr = intent.GetStringExtra("longitude") ?? "0";

                double lat = Convert.ToDouble(latStr.Replace(",", "."), CultureInfo.InvariantCulture);
                double lng = Convert.ToDouble(lngStr.Replace(",", "."), CultureInfo.InvariantCulture);

                Task.Run(async () =>
                {
                    await Task.Delay(1500); 
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DashboardPage.OpenOrUpdateEmergencyMap(targetUserId, targetUserName, lat, lng);
                    });
                });

                intent.RemoveExtra("openMap");
            }
        }
    }
}