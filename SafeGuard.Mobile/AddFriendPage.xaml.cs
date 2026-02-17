using SafeGuard.Mobile.Services;

namespace SafeGuard.Mobile
{
    public partial class AddFriendPage : ContentPage
    {
        private readonly AuthService _authService;

        public AddFriendPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void OnSendRequestClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PhoneEntry.Text))
            {
                await DisplayAlert("Hata", "Numara giriniz.", "Tamam");
                return;
            }

            int myUserId = Preferences.Get("CurrentUserId", 0);

            if (myUserId == 0)
            {
                await DisplayAlert("Hata", "Oturum hatası. Lütfen tekrar giriş yapın.", "Tamam");
                return;
            }

            bool result = await _authService.SendFriendRequestAsync(myUserId, PhoneEntry.Text);

            if (result)
            {
                await DisplayAlert("Başarılı", "Arkadaşlık isteği gönderildi.", "Tamam");
                await Navigation.PopAsync(); // Sayfayı kapat
            }
            else
            {
                await DisplayAlert("Hata", "Kullanıcı bulunamadı veya zaten ekli.", "Tamam");
            }
        }
    }
}