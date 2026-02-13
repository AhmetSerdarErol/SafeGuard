using SafeGuard.Mobile.Models;
using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AuthService _authService;

        public RegisterPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Uyarı", "Lütfen tüm zorunlu alanları doldurun.", "Tamam");
                return;
            }

            LoadingSpinner.IsRunning = true;
            RegisterBtn.IsEnabled = false;

            try
            {
                var newUser = new User
                {
                    FullName = NameEntry.Text,
                    Email = EmailEntry.Text,
                    PhoneNumber = PhoneEntry.Text ?? "",
                    Password = PasswordEntry.Text
                };

                // ARTIK HATA VERMEYECEK: AuthService artık bu nesneyi kabul ediyor
                var (isSuccess, errorMessage) = await _authService.RegisterAsync(newUser);

                if (isSuccess)
                {
                    await DisplayAlert("Başarılı", "Personel kaydı tamamlandı.", "Tamam");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Kayıt Hatası", errorMessage ?? "İşlem reddedildi.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Sistem Hatası", ex.Message, "Tamam");
            }
            finally
            {
                LoadingSpinner.IsRunning = false;
                RegisterBtn.IsEnabled = true;
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}