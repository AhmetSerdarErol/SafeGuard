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

            // Eğer InitialsConverter burada yoksa DashboardPage'den de alabilir veya buraya ekleyebilirsin.
            // Ama App.xaml içinde ResourceDictionary olarak tanımlamak en iyisidir.
            // Şimdilik hata vermemesi için buraya eklemiyorum, Dashboard'da zaten var.
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
            // Butondan gelen veriyi güvenli bir şekilde alıyoruz
            var button = sender as Button;

            // "is int requestId" diyerek, gelen şeyin sayı olup olmadığını kontrol ediyoruz
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
                // Listeden kaldır
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
    }
}