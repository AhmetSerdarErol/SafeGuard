using System.Text.RegularExpressions;
using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AuthService _authService = new AuthService();

        public RegisterPage()
        {
            InitializeComponent();
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            // Hataları gizle
            ErrorLabelStep1.IsVisible = false;

            // 1. BOŞLUK KONTROLÜ (Zorunlu Alanlar)
            if (string.IsNullOrWhiteSpace(FullNameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PhoneEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ShowError(ErrorLabelStep1, "Lütfen tüm temel bilgileri doldurun.");
                return;
            }

            // 2. EMAIL FORMATI (Regex)
            if (!Regex.IsMatch(EmailEntry.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError(ErrorLabelStep1, "Geçerli bir e-posta adresi giriniz.");
                return;
            }

            // 3. TELEFON DOĞRULAMA (TR Standartı: 05xx...)
            string phone = PhoneEntry.Text.Replace(" ", "").Replace("-", "");
            if (phone.Length != 11 || !phone.StartsWith("05"))
            {
                ShowError(ErrorLabelStep1, "Telefon 05 ile başlamalı ve 11 hane olmalı.");
                return;
            }

            // 4. ŞİFRE UZUNLUĞU
            if (PasswordEntry.Text.Length < 6)
            {
                ShowError(ErrorLabelStep1, "Şifre en az 6 karakter olmalıdır.");
                return;
            }

            // Her şey tamamsa Adım 2'ye geç
            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = true;
            ProgressStep1.Color = Color.FromArgb("#333333");
            ProgressStep2.Color = Color.FromArgb("#512BD4");
            StepLabel.Text = "Adım 2 / 2: Sağlık Bilgileri";
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            Step1Layout.IsVisible = true;
            Step2Layout.IsVisible = false;
            ProgressStep1.Color = Color.FromArgb("#512BD4");
            ProgressStep2.Color = Color.FromArgb("#333333");
            StepLabel.Text = "Adım 1 / 2: Temel Bilgiler";
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            ErrorLabelStep2.IsVisible = false;

            // KAN GRUBU ZORUNLULUĞU
            if (BloodTypePicker.SelectedIndex == -1)
            {
                ShowError(ErrorLabelStep2, "Lütfen kan grubunuzu seçin.");
                return;
            }

            // OPSİYONEL ALANLAR (Boşsa "Belirtilmedi" olarak gider)
            string diseases = string.IsNullOrWhiteSpace(DiseasesEntry.Text) ? "Belirtilmedi" : DiseasesEntry.Text;
            string allergies = string.IsNullOrWhiteSpace(AllergiesEntry.Text) ? "Belirtilmedi" : AllergiesEntry.Text;

            // KAYIT İŞLEMİ
            var result = await _authService.RegisterAsync(
                FullNameEntry.Text,
                UsernameEntry.Text,
                EmailEntry.Text,
                PasswordEntry.Text,
                PhoneEntry.Text.Replace(" ", ""),
                BloodTypePicker.SelectedItem.ToString(),
                diseases,
                allergies,
                SmokeCheck.IsChecked,
                AlcoholCheck.IsChecked
            );

            if (result.IsSuccess)
            {
                await DisplayAlert("Hoş Geldiniz", $"Sayın {FullNameEntry.Text}, kaydınız başarıyla tamamlandı.", "Tamam");
                await Navigation.PopAsync();
            }
            else
            {
                ShowError(ErrorLabelStep2, result.ErrorMessage);
            }
        }

        // Hata gösterme yardımcısı
        private void ShowError(Label errorLabel, string message)
        {
            errorLabel.Text = $"❌ {message}";
            errorLabel.IsVisible = true;
        }
    }
}