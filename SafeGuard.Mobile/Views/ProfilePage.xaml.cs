using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class ProfilePage : ContentPage
    {
        private readonly AuthService _authService = new AuthService();
        private int currentUserId;

        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadData();
        }

        private void LoadData()
        {
            currentUserId = Preferences.Get("CurrentUserId", 0);
            NameEntry.Text = Preferences.Get("UserFullName", "");
            PhoneEntry.Text = Preferences.Get("UserPhone", "");
            BloodEntry.Text = Preferences.Get("UserBlood", "");

            string photoUrl = Preferences.Get("UserPhotoUrl", "");
            if (!string.IsNullOrEmpty(photoUrl))
            {
                ProfileImage.Source = $"http://10.0.2.2:5161/{photoUrl}";
                InitialsLabel.IsVisible = false;
            }
            else
            {
                InitialsLabel.IsVisible = true;
                InitialsLabel.Text = NameEntry.Text.Length >= 2 ? NameEntry.Text.Substring(0, 2).ToUpper() : "?";
            }
        }

        private async void OnChangePhotoClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("⚠️ ÖNEMLİ UYARI",
                "Güvenliğiniz için lütfen YÜZÜNÜZÜN NET GÖRÜNDÜĞÜ bir fotoğraf yükleyiniz. Acil durumda sizi tanımamız için bu şarttır.",
                "ANLADIM & SEÇ", "İPTAL");

            if (answer)
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result != null)
                {
                    // Ekranda göster
                    var stream = await result.OpenReadAsync();
                    ProfileImage.Source = ImageSource.FromStream(() => stream);
                    InitialsLabel.IsVisible = false;

                    // Yükle ve Kaydet
                    string status = await _authService.UploadProfilePhotoAsync(currentUserId, result);
                    if (status == "OK")
                    {
                        // Geçici olarak kaydet ki Dashboard görsün
                        // Gerçekte sunucudan gelen URL'i kaydetmek daha doğrudur
                        await DisplayAlert("Başarılı", "Fotoğraf yüklendi (Lütfen sayfayı yenileyin)", "Tamam");
                    }
                }
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            bool success = await _authService.UpdateProfileInfoAsync(currentUserId, NameEntry.Text, PhoneEntry.Text, BloodEntry.Text);
            if (success)
            {
                Preferences.Set("UserFullName", NameEntry.Text);
                Preferences.Set("UserPhone", PhoneEntry.Text);
                Preferences.Set("UserBlood", BloodEntry.Text);
                await DisplayAlert("Tamam", "Bilgiler Güncellendi", "OK");
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Clear();
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
}