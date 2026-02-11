using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage()
    {
        InitializeComponent();
        _authService = new AuthService(); // Servisi başlatıyoruz
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // 1. Boş alan kontrolü
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        // 2. Yükleniyor animasyonunu aç
        LoadingSpinner.IsVisible = true;
        LoadingSpinner.IsRunning = true;

        // 3. Kayıt İsteği Gönder (Username, Email, Password)
        var result = await _authService.RegisterAsync(UsernameEntry.Text, EmailEntry.Text, PasswordEntry.Text);

        // 4. Animasyonu kapat
        LoadingSpinner.IsRunning = false;
        LoadingSpinner.IsVisible = false;

        if (result.IsSuccess)
        {
            // Başarılıysa bilgi ver ve Giriş sayfasına yönlendir
            await DisplayAlert("Başarılı", "Hesabınız oluşturuldu! Şimdi giriş yapabilirsiniz.", "Tamam");
            await Navigation.PopAsync(); // Geri (Giriş) sayfasına dön
        }
        else
        {
            // Hata varsa göster (Örn: Bu e-posta zaten kayıtlı)
            await DisplayAlert("Kayıt Başarısız", result.ErrorMessage, "Tamam");
        }
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        // "Zaten hesabım var"a basınca giriş sayfasına dön
        await Navigation.PopAsync();
    }
}