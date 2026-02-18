using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly AuthService _authService;

        public MainPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EmailEntry.Text) || string.IsNullOrEmpty(PasswordEntry.Text))
            {
                await DisplayAlert("Hata", "Lütfen bilgilerinizi girin.", "Tamam");
                return;
            }

            LoadingSpinner.IsRunning = true;
            LoginBtn.IsEnabled = false;

            var result = await _authService.LoginAsync(EmailEntry.Text, PasswordEntry.Text);

            LoadingSpinner.IsRunning = false;
            LoginBtn.IsEnabled = true;

            if (result.IsSuccess)
            {
                // Kullanıcı bilgilerini telefona kaydet
                Preferences.Set("UserFullName", result.FullName);
                Preferences.Set("CurrentUserId", result.UserId);

                Application.Current.MainPage = new NavigationPage(new DashboardPage());
            }
            else
            {
                await DisplayAlert("Giriş Yapılamadı", result.ErrorMessage, "Tamam");
            }
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}