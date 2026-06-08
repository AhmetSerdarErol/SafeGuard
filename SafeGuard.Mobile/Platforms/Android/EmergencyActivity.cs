    using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AScreenOrientation = Android.Content.PM.ScreenOrientation;
using AMediaPlayer = Android.Media.MediaPlayer;
using AColor = Android.Graphics.Color;
using AButton = Android.Widget.Button;
using AOrientation = Android.Widget.Orientation;
using ATypefaceStyle = Android.Graphics.TypefaceStyle;
using MyResource = SafeGuard.Mobile.Resource;

namespace SafeGuard.Mobile.Platforms.Android
{
    [Activity(Label = "Acil Durum", Theme = "@style/Maui.SplashTheme", ScreenOrientation = AScreenOrientation.Portrait)]
    public class EmergencyActivity : Activity
    {
        AMediaPlayer mediaPlayer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.OMr1)
            {
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
                var keyguardManager = (global::Android.App.KeyguardManager)GetSystemService(KeyguardService);
                keyguardManager?.RequestDismissKeyguard(this, null);
            }
            else
            {
                Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked |
                                global::Android.Views.WindowManagerFlags.TurnScreenOn |
                                global::Android.Views.WindowManagerFlags.KeepScreenOn |
                                global::Android.Views.WindowManagerFlags.DismissKeyguard);
            }

            string senderName = Intent.GetStringExtra("senderName") ?? "BİR YAKININIZ";

            var mainLayout = new LinearLayout(this)
            {
                Orientation = AOrientation.Vertical,
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            mainLayout.SetBackgroundColor(AColor.ParseColor("#B71C1C"));
            mainLayout.SetGravity(GravityFlags.Center);
            mainLayout.SetPadding(50, 50, 50, 50);

            senderName = Intent.GetStringExtra("YardimIsteyenKisi") ?? "BİR YAKININIZ";
            var nameText = new TextView(this)
            {
                Text = senderName.ToUpper(),
                TextSize = 42f
            };
            nameText.SetTextColor(AColor.White);
            nameText.SetTypeface(null, ATypefaceStyle.Bold);
            nameText.Gravity = GravityFlags.Center;

            var alertText = new TextView(this)
            {
                Text = "SİZDEN ACİL YARDIM İSTİYOR!",
                TextSize = 20f
            };
            alertText.SetTextColor(AColor.ParseColor("#FFCDD2"));
            alertText.SetTypeface(null, ATypefaceStyle.Bold);
            alertText.Gravity = GravityFlags.Center;
            alertText.SetPadding(0, 20, 0, 120);

            var stopButton = new AButton(this)
            {
                Text = "SESİ SUSTUR VE KONUMA GİT",
                TextSize = 16f
            };
            stopButton.SetBackgroundColor(AColor.White);
            stopButton.SetTextColor(AColor.ParseColor("#B71C1C"));
            stopButton.SetPadding(40, 50, 40, 50);

            mainLayout.AddView(nameText);
            mainLayout.AddView(alertText);
            mainLayout.AddView(stopButton);

            SetContentView(mainLayout);

            mediaPlayer = AMediaPlayer.Create(this, MyResource.Raw.siren);
            if (mediaPlayer != null)
            {
                mediaPlayer.Looping = true;
                mediaPlayer.Start();
            }

            stopButton.Click += (sender, e) =>
            {

                string myName = Microsoft.Maui.Storage.Preferences.Get("UserFullName", "Bir Yardımsever");
                string targetUserId = Intent.GetStringExtra("YardimIsteyenId") ?? "";

                string latStr = Intent.GetStringExtra("latitude") ?? "0";
                string lngStr = Intent.GetStringExtra("longitude") ?? "0";
                string senderName = Intent.GetStringExtra("YardimIsteyenKisi") ?? "BİR YAKININIZ";

                if (!string.IsNullOrEmpty(targetUserId))
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var signalR = new SafeGuard.Mobile.Services.SignalRService();
                            await signalR.ConfirmHelp(myName, targetUserId);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Yardım iletme hatası: " + ex.Message);
                        }
                    });
                }

                if (mediaPlayer != null)
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Release();
                    mediaPlayer = null;
                }

                try
                {
                    var context = global::Android.App.Application.Context;
                    var appIntent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);

                    if (appIntent != null)
                    {
                        appIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                        appIntent.PutExtra("openMap", true);
                        appIntent.PutExtra("targetUserId", targetUserId);
                        appIntent.PutExtra("targetUserName", senderName);
                        appIntent.PutExtra("latitude", latStr);
                        appIntent.PutExtra("longitude", lngStr);

                        StartActivity(appIntent);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Uygulama açılırken hata: " + ex.Message);
                }

                Finish();
            };
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Release();
                mediaPlayer = null;
            }
        }
    }
}