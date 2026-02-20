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

        // --- SENİN 2 AŞAMALI GEÇİŞ KODLARIN (KORUNDU) ---
        private void OnNextStepClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                DisplayAlert("Eksik Bilgi", "Lütfen kişisel bilgileri (Ad, E-posta, Şifre) doldurun.", "Tamam");
                return;
            }

            Step1Layout.IsVisible = false;
            Step2Layout.IsVisible = true;
        }

        private void OnBackStepClicked(object sender, EventArgs e)
        {
            Step2Layout.IsVisible = false;
            Step1Layout.IsVisible = true;
        }
        private void OnOrganStatusChanged(object sender, EventArgs e)
        {
            if (OrganStatusPicker.SelectedIndex > 0) // "Yok" harici bir şey seçilirse
            {
                OrganDetailsEntry.IsVisible = true;
            }
            else
            {
                OrganDetailsEntry.IsVisible = false;
                OrganDetailsEntry.Text = "";
            }
        }
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            LoadingSpinner.IsRunning = true;
            RegisterBtn.IsEnabled = false;

            try
            {
                // UI verilerini toparla
                
                string smokeStatus = SmokeYes.IsChecked ? "Kullanıyorum" : "Kullanmıyorum";
                string alcoholStatus = AlcoholYes.IsChecked ? "Düzenli" : "Kullanmıyorum";
                string birthDate = string.Format("{0:dd/MM/yyyy}", BirthDatePicker.Date);
                string bloodType = BloodTypePicker.SelectedItem?.ToString() ?? "Belirtilmemiş";
                
                // YENİ DTO KUTUMUZU EKRANDAKİ VERİLERLE DOLDURUYORUZ
                var registerDto = new UserRegisterDto
                {
                    FullName = NameEntry.Text,
                    Email = EmailEntry.Text,
                    PhoneNumber = PhoneEntry.Text ?? "",
                    Password = PasswordEntry.Text,

                    BloodType = bloodType,
                    BirthDate = birthDate,
                    MedicalConditions = DiseasesEntry?.Text ?? "",
                    Allergies = AllergiesEntry?.Text ?? "",

                    SmokingHabit = smokeStatus,
                    AlcoholUse = alcoholStatus,

                    // Eğer ekranda boy/kilo girdisi yoksa null bırakıyoruz
                    Height = null,
                    Weight = null,
                    OrganStatus = "Yok",
                    OrganDetails = ""
                };

                // Servise gönder (Artık sadece true/false dönüyor)
                bool isSuccess = await _authService.RegisterAsync(registerDto);

                if (isSuccess)
                {
                    await DisplayAlert("Başarılı", "Kayıt tamamlandı.", "Tamam");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Kayıt Hatası", "İşlem başarısız. Bu e-posta kullanımda olabilir.", "Tamam");
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