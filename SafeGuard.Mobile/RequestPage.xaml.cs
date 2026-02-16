using SafeGuard.Mobile.Services;
using SafeGuard.Mobile.Models;

namespace SafeGuard.Mobile
{
    public partial class RequestsPage : ContentPage
    {
        private readonly AuthService _authService;
        private int _myUserId; // Giriş yapan kullanıcının ID'si

        public RequestsPage()
        {
            InitializeComponent();
            _authService = new AuthService();
            // Not: Normalde ID'yi giriş yaparken bir yere (SecureStorage veya static değişken) kaydetmelisin.
            // Şimdilik örnek olarak 1 varsayalım veya Login'den parametre alabilirsin.
            _myUserId = 1; // BURAYI KENDİ SİSTEMİNE GÖRE DÜZELT (LoginUser.Id)
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRequests();
        }

        private async Task LoadRequests()
        {
            var requests = await _authService.GetPendingRequestsAsync(_myUserId);

            if (requests.Count == 0)
            {
                NoRequestLabel.IsVisible = true;
                RequestsList.IsVisible = false;
            }
            else
            {
                NoRequestLabel.IsVisible = false;
                RequestsList.IsVisible = true;
                RequestsList.ItemsSource = requests;
            }
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            int requestId = (int)button.CommandParameter;

            bool success = await _authService.RespondToRequestAsync(requestId, true);
            if (success)
            {
                await DisplayAlert("Harika", "İstek kabul edildi! Artık bu kişinin güvende olmasına yardımcı olacaksınız.", "Tamam");
                await LoadRequests(); // Listeyi yenile
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            int requestId = (int)button.CommandParameter;

            bool success = await _authService.RespondToRequestAsync(requestId, false);
            if (success)
            {
                await LoadRequests(); // Listeyi yenile
            }
        }
    }
}