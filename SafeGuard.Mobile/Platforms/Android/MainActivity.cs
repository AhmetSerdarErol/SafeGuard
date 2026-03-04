using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.CloudMessaging;

namespace SafeGuard.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Uygulama ilk açıldığında veya kapalıyken bildirime tıklandığında çalışır
        FirebaseCloudMessagingImplementation.OnNewIntent(Intent);
    }

    protected override void OnNewIntent(Android.Content.Intent intent)
    {
        base.OnNewIntent(intent);

        // Uygulama açıkken (arka planda çalışırken) bildirim geldiğinde çalışır
        FirebaseCloudMessagingImplementation.OnNewIntent(intent);
    }
}