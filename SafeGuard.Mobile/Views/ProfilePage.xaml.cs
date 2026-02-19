using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly AuthService _authService = new AuthService();
    private int currentUserId;
    private bool isEditMode = false;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserData(); 
    }

   
    private void LoadUserData()
    {
        currentUserId = Preferences.Get("CurrentUserId", 0);

        NameEntry.Text = Preferences.Get("UserFullName", "");
        PhoneEntry.Text = Preferences.Get("UserPhone", "");
        HeightEntry.Text = Preferences.Get("UserHeight", "");
        WeightEntry.Text = Preferences.Get("UserWeight", "");
        BloodPicker.SelectedItem = Preferences.Get("UserBlood", null);

        ConditionsEntry.Text = Preferences.Get("UserConditions", "");
        AllergiesEntry.Text = Preferences.Get("UserAllergies", "");
        MedicationsEntry.Text = Preferences.Get("UserMedications", "");

        OrganStatusPicker.SelectedItem = Preferences.Get("UserOrganStatus", "Yok");
        OrganDetailsEntry.Text = Preferences.Get("UserOrganDetails", "");
        AlcoholPicker.SelectedItem = Preferences.Get("UserAlcohol", null);
        SmokingPicker.SelectedItem = Preferences.Get("UserSmoking", null);

        string photoUrl = Preferences.Get("UserPhotoUrl", "");
        if (!string.IsNullOrEmpty(photoUrl))
        {
            ProfileImage.Source = $"http://10.0.2.2:5161/{photoUrl}";
            InitialsLabel.IsVisible = false;
        }
        else
        {
            ProfileImage.Source = null;
            InitialsLabel.IsVisible = true;
            string fullName = NameEntry.Text ?? "U";
            InitialsLabel.Text = fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : fullName.Substring(0, 1).ToUpper();
        }
    }

    private void OnOrganStatusChanged(object sender, EventArgs e)
    {
        // Eğer "Yok" seçilmediyse (yani Nakil aldıysa veya bağışladıysa) detay kutusunu göster
        if (OrganStatusPicker.SelectedIndex == 1 || OrganStatusPicker.SelectedIndex == 2)
        {
            OrganDetailsEntry.IsVisible = true;
        }
        else
        {
            OrganDetailsEntry.IsVisible = false;
            OrganDetailsEntry.Text = ""; // Kapatılırsa içini temizle
        }
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        isEditMode = !isEditMode;

        // 1. Kutuların Kilidini Aç / Kapat
        NameEntry.IsReadOnly = !isEditMode;
        PhoneEntry.IsReadOnly = !isEditMode;
        HeightEntry.IsReadOnly = !isEditMode;
        WeightEntry.IsReadOnly = !isEditMode;
        ConditionsEntry.IsReadOnly = !isEditMode;
        AllergiesEntry.IsReadOnly = !isEditMode;
        MedicationsEntry.IsReadOnly = !isEditMode;
        OrganStatusPicker.IsEnabled = isEditMode;
        OrganDetailsEntry.IsReadOnly = !isEditMode;
        // 2. Seçicilerin Kilidini Aç / Kapat
        BloodPicker.IsEnabled = isEditMode;
        AlcoholPicker.IsEnabled = isEditMode;
        SmokingPicker.IsEnabled = isEditMode;

        // 3. Görünümü Değiştir
        if (isEditMode)
        {
            EditButton.Text = "İptal Et ✖";
            EditButton.TextColor = Colors.Gray;
            SaveContainer.IsVisible = true;
            LogoutButton.IsVisible = false;
            PhotoChangeHint.IsVisible = true;
        }
        else
        {
            EditButton.Text = "Düzenle ✎";
            EditButton.TextColor = Color.FromArgb("#FF3B30");
            SaveContainer.IsVisible = false;
            LogoutButton.IsVisible = true;
            PhotoChangeHint.IsVisible = false;
            LoadUserData(); // Kullanıcı iptale basarsa değişiklikleri sil, orijinali geri getir
        }
    }

    // KAYDET BUTONUNA BASILINCA (Gerçek Backend Bağlantısı)
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // AuthService içindeki metodu çağırıyoruz
        bool success = await _authService.UpdateFullProfileInfoAsync(
            currentUserId,
            NameEntry.Text,
            PhoneEntry.Text,
            HeightEntry.Text,
            WeightEntry.Text,
            BloodPicker.SelectedItem?.ToString(),
            ConditionsEntry.Text,
            AllergiesEntry.Text,
            MedicationsEntry.Text,
            OrganStatusPicker.SelectedItem?.ToString(), // YENİ
            OrganDetailsEntry.Text,                     // YENİ
            AlcoholPicker.SelectedItem?.ToString(),
            SmokingPicker.SelectedItem?.ToString()
        );

        if (success)
        {
            // Veritabanı güncellendi, şimdi telefon hafızasını güncelleyelim
            Preferences.Set("UserFullName", NameEntry.Text);
            Preferences.Set("UserPhone", PhoneEntry.Text);
            Preferences.Set("UserHeight", HeightEntry.Text);
            Preferences.Set("UserWeight", WeightEntry.Text);
            Preferences.Set("UserBlood", BloodPicker.SelectedItem?.ToString());
            Preferences.Set("UserConditions", ConditionsEntry.Text);
            Preferences.Set("UserAllergies", AllergiesEntry.Text);
            Preferences.Set("UserMedications", MedicationsEntry.Text);
            Preferences.Set("UserOrganStatus", OrganStatusPicker.SelectedItem?.ToString());
            Preferences.Set("UserOrganDetails", OrganDetailsEntry.Text);
            Preferences.Set("UserAlcohol", AlcoholPicker.SelectedItem?.ToString());
            Preferences.Set("UserSmoking", SmokingPicker.SelectedItem?.ToString());

            await DisplayAlert("Başarılı", "Tıbbi kimlik ve profil bilgileriniz güvenle kaydedildi.", "Tamam");

            // Kaydettikten sonra kilitli (görüntüleme) moduna geri dön
            isEditMode = true;
            OnEditClicked(null, null);
        }
        else
        {
            await DisplayAlert("Hata", "Bilgiler sunucuya kaydedilirken bir sorun oluştu.", "Tamam");
        }
    }

    // FOTOĞRAF DEĞİŞTİRME (Sadece düzenleme modunda çalışır)
    private async void OnChangePhotoClicked(object sender, EventArgs e)
    {
        if (!isEditMode) return; // Kilitliyse işlem yapma

        bool anladim = await DisplayAlert(
            "⚠️ GÜVENLİK UYARISI",
            "Acil durumlarda kimliğinizin hızlı tespiti için lütfen YÜZÜNÜZÜN NET GÖRÜNDÜĞÜ bir fotoğraf yükleyiniz.\n\nKaranlık veya bulanık fotoğraflar güvenliğinizi riske atabilir.",
            "ANLADIM & SEÇ",
            "İPTAL");

        if (anladim)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result != null)
                {
                    // Seçilen resmi anında ekranda göster
                    var stream = await result.OpenReadAsync();
                    ProfileImage.Source = ImageSource.FromStream(() => stream);
                    InitialsLabel.IsVisible = false;

                    // Arka planda sunucuya fırlat
                    string status = await _authService.UploadProfilePhotoAsync(currentUserId, result);

                    if (status == "OK")
                    {
                        await DisplayAlert("Başarılı", "Profil fotoğrafınız güncellendi.", "Tamam");
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Sunucuya yüklenemedi.", "Tamam");
                        LoadUserData(); // Hata verirse eski fotoğrafa geri dön
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Galeriye erişilemedi: " + ex.Message, "Tamam");
            }
        }
    }

    // ÇIKIŞ YAP (Oturumu kapatır, her şeyi temizler)
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Çıkış Yap", "Sistemden çıkış yapmak istediğinize emin misiniz?", "Evet", "Hayır");
        if (answer)
        {
            Preferences.Clear(); // Telefon hafızasını tamamen siler
            Application.Current.MainPage = new NavigationPage(new MainPage()); // Veya LoginPage
        }
    }
}