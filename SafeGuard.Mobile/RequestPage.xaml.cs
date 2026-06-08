using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;
using System.Collections.ObjectModel;

namespace SafeGuard.Mobile
{
    public partial class RequestsPage : ContentPage
    {
        private readonly AuthService _authService;
        private ObservableCollection<RequestModel> _requests;

        public RequestsPage()
        {
            InitializeComponent();
            _authService = new AuthService();

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRequests();
        }

        private async Task LoadRequests()
        {
            LoadingSpinner.IsRunning = true;
            LoadingSpinner.IsVisible = true;
            EmptyLabel.IsVisible = false;
            RequestsCollection.IsVisible = false;

            int userId = Preferences.Get("CurrentUserId", 0);
            var requests = await _authService.GetPendingRequestsAsync(userId);

            LoadingSpinner.IsRunning = false;
            LoadingSpinner.IsVisible = false;

            if (requests != null && requests.Count > 0)
            {
                _requests = new ObservableCollection<RequestModel>(requests);
                RequestsCollection.ItemsSource = _requests;
                RequestsCollection.IsVisible = true;
            }
            else
            {
                EmptyLabel.IsVisible = true;
            }
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            var button = sender as Button;

            if (button != null && button.CommandParameter is int requestId)
            {
                await ProcessRequest(requestId, true);
            }
            else
            {
                await DisplayAlert("Hata", "İstek ID'si okunamadı.", "Tamam");
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var button = sender as Button;

            if (button != null && button.CommandParameter is int requestId)
            {
                await ProcessRequest(requestId, false);
            }
            else
            {
                await DisplayAlert("Hata", "İstek ID'si okunamadı.", "Tamam");
            }
        }

        private async Task ProcessRequest(int requestId, bool accept)
        {
            bool success = await _authService.RespondToRequestAsync(requestId, accept);
            if (success)
            {
                var item = _requests.FirstOrDefault(r => r.Id == requestId);
                if (item != null) _requests.Remove(item);

                if (_requests.Count == 0)
                {
                    EmptyLabel.IsVisible = true;
                    RequestsCollection.IsVisible = false;
                }

                string msg = accept ? "Kişi eklendi." : "İstek reddedildi.";
                await DisplayAlert("Bilgi", msg, "Tamam");
            }
            else
            {
                await DisplayAlert("Hata", "İşlem başarısız.", "Tamam");
            }
        }
        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                if (Navigation.ModalStack.Count > 0)
                    await Navigation.PopModalAsync();
                else
                    await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geri dönme hatası: {ex.Message}");
            }
        }
    }
}