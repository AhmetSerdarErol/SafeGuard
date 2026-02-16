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

        // --- 1. EKSİK OLAN GEÇİŞ KODLARI (BURAYI EKLEMEZSEN SAYFA DEĞİŞMEZ) ---
        private void OnNextStepClicked(object sender, EventArgs e)
        {
            // Temel alanlar boş mu kontrol et
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                DisplayAlert("Eksik Bilgi", "Lütfen kişisel bilgileri (Ad, E-posta, Şifre) doldurun.", "Tamam");
                return;
            }

            // 1. Ekranı Kapat, 2. Ekranı Aç
            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = true;
        }

        private void OnBackStepClicked(object sender, EventArgs e)
        {
            // Geri dönünce 2. Ekranı Kapat, 1. Ekranı Aç
            Step2Layout.IsVisible = false;
            Step1Layout.IsVisible = true;
        }
        // -------------------------------------------------------------------

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            LoadingSpinner.IsRunning = true;
            RegisterBtn.IsEnabled = false;

            try
            {
                // 1. Kan Grubunu Picker'dan al (Seçilmediyse boş string olsun)
                string selectedBloodType = BloodTypePicker.SelectedItem?.ToString() ?? "";

                // 2. Alışkanlıkları RadioButton'lardan birleştir
                // Backend tek bir string beklediği için biz bunları birleştirip gönderiyoruz.
                List<string> habitList = new List<string>();

                if (SmokeYes.IsChecked) habitList.Add("Sigara");
                if (AlcoholYes.IsChecked) habitList.Add("Alkol");

                string habitsString = habitList.Count > 0 ? string.Join(", ", habitList) : "Yok";

                // 3. Kullanıcı Nesnesini Oluştur
                var newUser = new User
                {
                    FullName = NameEntry.Text,
                    Email = EmailEntry.Text,
                    PhoneNumber = PhoneEntry.Text ?? "",
                    Password = PasswordEntry.Text,

                    // YENİ UI VERİLERİ:
                    BloodType = selectedBloodType,
                    Habits = habitsString,

                    // DİĞERLERİ:
                    Allergies = AllergiesEntry?.Text ?? "",
                    ChronicDiseases = DiseasesEntry?.Text ?? "",
                    Surgeries = SurgeriesEntry?.Text ?? ""
                };

                var (isSuccess, errorMessage) = await _authService.RegisterAsync(newUser);

                if (isSuccess)
                {
                    await DisplayAlert("Başarılı", "Kayıt tamamlandı.", "Tamam");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Kayıt Hatası", errorMessage ?? "İşlem başarısız.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
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