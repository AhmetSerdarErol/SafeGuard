using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents; // Firebase'i başlatmak için yaşam döngüsü kütüphanesi

#if ANDROID
using Plugin.Firebase.Core.Platforms.Android; // Sadece Android'de çalışacak Firebase paketi
#endif

namespace SafeGuard.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                // EKLENEN KISIM: Uygulama açılırken Firebase motorunu ateşler
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android => android.OnCreate((activity, state) =>
                        CrossFirebase.Initialize(activity, () => activity)));
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}