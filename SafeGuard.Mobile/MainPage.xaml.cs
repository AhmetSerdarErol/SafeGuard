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
                await DisplayAlert("Hata", "Lütfen e-posta ve şifrenizi girin.", "Tamam");
                return;
            }

            // Yükleniyor...
            LoadingSpinner.IsRunning = true;
            LoginBtn.IsEnabled = false;
            LoginBtn.Text = "Bağlanılıyor...";

            // --- YENİ KISIM: Hem sonucu hem hatayı alıyoruz ---
            var sonuc = await _authService.LoginAsync(EmailEntry.Text, PasswordEntry.Text);

            // İşlem bitti
            LoadingSpinner.IsRunning = false;
            LoginBtn.IsEnabled = true;
            LoginBtn.Text = "GİRİŞ YAP";

            if (sonuc.IsSuccess)
            {
                // Başarılıysa geç
                await Navigation.PushModalAsync(new DashboardPage());
            }
            else
            {
                // BAŞARISIZSA HATAYI GÖSTER!
                // Buradaki mesaj bize sorunun ne olduğunu tam olarak söyleyecek.
                await DisplayAlert("Giriş Yapılamadı", sonuc.ErrorMessage, "Tamam");
            }
        }
        // MainPage.xaml.cs içine ekle:
        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            // Kayıt sayfasına git
            await Navigation.PushAsync(new RegisterPage());
        }

    }
}