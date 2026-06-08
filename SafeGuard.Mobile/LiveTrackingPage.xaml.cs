using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.AspNetCore.SignalR.Client;
using SafeGuard.Mobile.Services;
namespace SafeGuard.Mobile.Views;

public partial class LiveTrackingPage : ContentPage
{
    private HubConnection _hubConnection;
    private Pin _targetPin;
    private string _targetUserId;

    public LiveTrackingPage(string targetUserId, double initialLat, double initialLng)
    {
        InitializeComponent();
        _targetUserId = targetUserId;

        SetupMap(initialLat, initialLng);
        InitializeSignalR();
    }

    private void SetupMap(double lat, double lng)
    {
        var initialLocation = new Location(lat, lng);

        _targetPin = new Pin
        {
            Label = "Acil Durum!",
            Type = PinType.Place,
            Location = initialLocation
        };

        TrackingMap.Pins.Add(_targetPin);
        TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(initialLocation, Distance.FromKilometers(1)));
    }

    private async void InitializeSignalR()
    {
        string hubUrl = "http://10.0.2.2:5000/locationHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        _hubConnection.On<string, double, double>("ReceiveLocationUpdate", (userId, lat, lng) =>
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var updatedLocation = new Location(lat, lng);

                _targetPin.Location = updatedLocation;

                TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(updatedLocation, Distance.FromKilometers(1)));

                Console.WriteLine($"[SİNYAL ALINDI] Harita güncellendi! Yeni Konum: {lat}, {lng}");
            });
        });

        _hubConnection.On<string, string>("ReceiveActionLog", (helperName, actionMessage) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddLogEntry($"[{DateTime.Now:HH:mm}] {helperName} {actionMessage}");
            });
        });

        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine("SignalR Bağlantısı Başarılı! Ajan Modu Aktif.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bağlantı hatası: {ex.Message}");
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        try
        {
            var location = _targetPin.Location;
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location.Latitude, location.Longitude, new MapLaunchOptions
            {
                Name = "Acil Durum Hedefi",
                NavigationMode = NavigationMode.Driving
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Navigasyon açılamadı: " + ex.Message, "Tamam");
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
        }

        await Navigation.PopModalAsync();
    }

    private async void OnCall112Clicked(object sender, EventArgs e)
    {
        BtnCall112.IsEnabled = false;
        BtnCall112.BackgroundColor = Colors.Gray;

        string userName = Preferences.Get("UserFullName", "Bir yardımsever");
        AddLogEntry($"📞 [{DateTime.Now:HH:mm}] {userName} 112 Acil Servis'i aradı.");

        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open("112");
            else
                await Launcher.OpenAsync("tel:112");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Arama ekranı açılamadı: " + ex.Message, "Tamam");
        }
    }

    private async void OnGoToSceneClicked(object sender, EventArgs e)
    {
        BtnGoScene.IsEnabled = false;
        BtnGoScene.BackgroundColor = Colors.Gray;

        string userName = Preferences.Get("UserFullName", "Bir yardımsever");
        AddLogEntry($"🏃 [{DateTime.Now:HH:mm}] {userName} olay yerine doğru yola çıktı.");

        try
        {
            var location = _targetPin.Location;
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location.Latitude, location.Longitude, new MapLaunchOptions
            {
                Name = "Acil Durum Hedefi",
                NavigationMode = NavigationMode.Driving
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Navigasyon açılamadı: " + ex.Message, "Tamam");
        }
    }

    private async void OnResolvedClicked(object sender, EventArgs e)
    {
        bool isResolved = await DisplayAlert("Tehlike Geçti", "Durumun çözüldüğünü onaylıyor musunuz?", "Evet", "İptal");

        if (isResolved)
        {
            try
            {
                var authService = new AuthService();
                await authService.CancelSosAsync(int.Parse(_targetUserId));

                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SendSafe", int.Parse(_targetUserId));
                }

                BtnResolved.IsEnabled = false;
                BtnResolved.BackgroundColor = Colors.Gray;
                AddLogEntry($"✅ [{DateTime.Now:HH:mm}] Durum çözüldü olarak işaretlendi.");
            }
            catch (Exception ex)    
            {
                await DisplayAlert("Hata", "Durum güncellenemedi: " + ex.Message, "Tamam");
            }
        }
    }

    private void AddLogEntry(string message)
    {
        var logLabel = new Label
        {
            Text = message,
            TextColor = Color.FromArgb("#A0A0A0"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 5)
        };

        ActionLogsStack.Children.Add(logLabel);

        LogScrollView.ScrollToAsync(ActionLogsStack, ScrollToPosition.End, true);
    }
}