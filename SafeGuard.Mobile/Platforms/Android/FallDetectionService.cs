using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.Devices.Sensors;
using Android.Content.PM;

namespace SafeGuard.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class FallDetectionService : Service
    {
        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();

            var notification = new NotificationCompat.Builder(this, "FallChannel")
                .SetContentTitle("SafeGuard Devrede")
                .SetContentText("Düşme ve çarpma sensörleri arka planda dinleniyor...")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
                .Build();
            
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Q)
            {
                StartForeground(1001, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(1001, notification);
            }

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
            catch (Exception) { }

            return StartCommandResult.Sticky;
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            double gForce = Math.Sqrt(data.Acceleration.X * data.Acceleration.X +
                                      data.Acceleration.Y * data.Acceleration.Y +
                                      data.Acceleration.Z * data.Acceleration.Z);

            if (gForce > 3.5) 
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;

                SafeGuard.Mobile.FallAlertSystem.OnFallDetected?.Invoke(gForce.ToString("F1"));
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("FallChannel", "Düşme Algılama", NotificationImportance.Low);
                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
    }
}