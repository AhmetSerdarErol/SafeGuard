using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Firebase.Messaging;
using Microsoft.Maui.Storage;

namespace SafeGuard.Mobile.Platforms.Android
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseService : FirebaseMessagingService
    {
        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            Preferences.Set("FcmToken", token);
            System.Diagnostics.Debug.WriteLine($"🚨 YENİ TOKEN ALINDI VE ÇEKMECEYE KONDU: {token}");
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            var powerManager = (global::Android.OS.PowerManager)GetSystemService(PowerService);
            if (powerManager != null)
            {
                var wakeLock = powerManager.NewWakeLock(
                    global::Android.OS.WakeLockFlags.ScreenBright |
                    global::Android.OS.WakeLockFlags.AcquireCausesWakeup,
                    "SafeGuard::EmergencyWakeLock");

                wakeLock.Acquire(5000);
            }

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);


            var soundUri = global::Android.Net.Uri.Parse($"android.resource://{PackageName}/raw/siren");
            string channelId = "sos_acil_kanal_v3";


            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "VIP Acil Durum SOS", NotificationImportance.High)
                {
                    Description = "Acil durum ve SOS bildirimleri için kullanılır.",
                    LockscreenVisibility = NotificationVisibility.Public
                };

                var audioAttributes = new global::Android.Media.AudioAttributes.Builder()
                    .SetContentType(global::Android.Media.AudioContentType.Sonification)
                    .SetUsage(global::Android.Media.AudioUsageKind.Alarm)
                    .Build();

                channel.SetSound(soundUri, audioAttributes);
                channel.SetBypassDnd(true);
                notificationManager.CreateNotificationChannel(channel);
            }

            var intent = new Intent(this, typeof(EmergencyActivity));
            string gonderenKisi = message.Data.ContainsKey("senderName") ? message.Data["senderName"] : "Bir Yakınınız";
            intent.PutExtra("YardimIsteyenKisi", gonderenKisi);

            string gonderenId = message.Data.ContainsKey("senderId") ? message.Data["senderId"] : "";
            intent.PutExtra("YardimIsteyenId", gonderenId);
            string latStr = message.Data.ContainsKey("latitude") ? message.Data["latitude"] : "0";
            string lngStr = message.Data.ContainsKey("longitude") ? message.Data["longitude"] : "0";
            intent.PutExtra("latitude", latStr);
            intent.PutExtra("longitude", lngStr);
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.ClearTop);

            var pendingIntentFlags = (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
                ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
                : PendingIntentFlags.UpdateCurrent;

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

            var notificationBuilder = new NotificationCompat.Builder(this, channelId)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
                .SetContentTitle("🚨 ACİL DURUM ÇAĞRISI!")
                .SetContentText($"{gonderenKisi} acil yardım bekliyor!")
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryAlarm)
                .SetSound(soundUri)
                .SetFullScreenIntent(pendingIntent, true)
                .SetAutoCancel(true);

            notificationManager.Notify(1, notificationBuilder.Build());
        }
    }
}