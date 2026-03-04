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
    // Ekranda üst barı (saat, şarj vs.) gizleyip tam sayfa yapıyoruz
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

            // Gelen ismi alıyoruz (Eğer isim gelmezse "BİR YAKININIZ" yazacak)
            string senderName = Intent.GetStringExtra("senderName") ?? "BİR YAKININIZ";

            // --- ANA EKRAN TASARIMI (Kan Kırmızısı, Ciddi ve Temiz) ---
            var mainLayout = new LinearLayout(this)
            {
                Orientation = AOrientation.Vertical, // Çakışma önlendi
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            mainLayout.SetBackgroundColor(AColor.ParseColor("#B71C1C")); // Çakışma önlendi
            mainLayout.SetGravity(GravityFlags.Center);
            mainLayout.SetPadding(50, 50, 50, 50);

            senderName = Intent.GetStringExtra("YardimIsteyenKisi") ?? "BİR YAKININIZ";  
            var nameText = new TextView(this)
            {
                Text = senderName.ToUpper(),
                TextSize = 42f
            };
            nameText.SetTextColor(AColor.White); // Çakışma önlendi
            nameText.SetTypeface(null, ATypefaceStyle.Bold); // Çakışma önlendi
            nameText.Gravity = GravityFlags.Center;

            // 2. UYARI METNİ
            var alertText = new TextView(this)
            {
                Text = "SİZDEN ACİL YARDIM İSTİYOR!",
                TextSize = 20f
            };
            alertText.SetTextColor(AColor.ParseColor("#FFCDD2"));
            alertText.SetTypeface(null, ATypefaceStyle.Bold);
            alertText.Gravity = GravityFlags.Center;
            alertText.SetPadding(0, 20, 0, 120);

            // 3. SUSTURMA BUTONU (Bembeyaz, dikkat çekici)
            var stopButton = new AButton(this) // Çakışma önlendi
            {
                Text = "SESİ SUSTUR VE KONUMA GİT",
                TextSize = 16f
            };
            stopButton.SetBackgroundColor(AColor.White);
            stopButton.SetTextColor(AColor.ParseColor("#B71C1C"));
            stopButton.SetPadding(40, 50, 40, 50);

            // Ekrana ekliyoruz
            mainLayout.AddView(nameText);
            mainLayout.AddView(alertText);
            mainLayout.AddView(stopButton);

            SetContentView(mainLayout);

            // --- SİREN SESİ KISMI ---
            mediaPlayer = AMediaPlayer.Create(this, MyResource.Raw.siren);
            if (mediaPlayer != null)
            {
                mediaPlayer.Looping = true;
                mediaPlayer.Start();
            }

            // BUTONA BASILDIĞINDA
            stopButton.Click += (sender, e) => {
                if (mediaPlayer != null)
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Release();
                    mediaPlayer = null;
                }
                Finish(); // Siyah ekranı kapatır, uygulamanın kendi sayfasına döner
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