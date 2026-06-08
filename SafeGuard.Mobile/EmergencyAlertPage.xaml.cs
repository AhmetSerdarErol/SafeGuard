using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class EmergencyAlertPage : ContentPage
    {
        private string _senderId;
        private string _senderName;
        private double _latitude;
        private double _longitude;
        private SignalRService _signalRService;

        private bool _isPageActive = false;

        public EmergencyAlertPage(string senderId, string senderName, double lat, double lon, SignalRService signalRService)
        {
            InitializeComponent();
            _senderId = senderId;
            _senderName = senderName;
            _latitude = lat;
            _longitude = lon;
            _signalRService = signalRService;

            SenderNameLabel.Text = _senderName;
            LocationLabel.Text = $"Konum: {_latitude:F4}, {_longitude:F4}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isPageActive = true;

            DeviceDisplay.Current.KeepScreenOn = true;

            _ = StartPulseAnimation();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isPageActive = false;

            DeviceDisplay.Current.KeepScreenOn = false;
        }

        private async Task StartPulseAnimation()
        {
            try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

            while (_isPageActive)
            {
                var pulseTask = SosIconBorder.ScaleTo(1.2, 500, Easing.SinOut);

                PulseRing1.Scale = 1; PulseRing1.Opacity = 0.8;
                PulseRing2.Scale = 1; PulseRing2.Opacity = 0.6;

                var ring1Task = Task.WhenAll(
                    PulseRing1.ScaleTo(3.0, 1500, Easing.SinOut),
                    PulseRing1.FadeTo(0, 1500, Easing.SinOut)
                );

                var ring2Task = Task.WhenAll(
                    PulseRing2.ScaleTo(2.5, 1500, Easing.SinOut),
                    PulseRing2.FadeTo(0, 1500, Easing.SinOut)
                );

                await Task.WhenAll(pulseTask, ring1Task, ring2Task);

                await SosIconBorder.ScaleTo(1.0, 500, Easing.SinIn);

                await Task.Delay(200);
            }
        }

        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);

            await Task.Delay(100);

            DashboardPage.OpenOrUpdateEmergencyMap(_senderId, _senderName, _latitude, _longitude);
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            string myName = Preferences.Get("UserFullName", "Bir Yardımsever");
            await _signalRService.ConfirmHelp(myName, _senderId);

            await Navigation.PopModalAsync(false);

            await Task.Delay(100);

            DashboardPage.OpenOrUpdateEmergencyMap(_senderId, _senderName, _latitude, _longitude);
        }

        private async void OnIgnoreClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}