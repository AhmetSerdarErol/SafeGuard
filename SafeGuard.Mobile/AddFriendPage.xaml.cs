using SafeGuard.Mobile.Services;
using System.Collections.ObjectModel;

namespace SafeGuard.Mobile.Views;

public partial class AddFriendPage : ContentPage
{
    private readonly AuthService _authService = new AuthService();
    private int currentUserId;

    // HATA 1 ÇÖZÜLDÜ: Modelin adını FriendUIModel yaptık ki seninkiyle çakışmasın
    public ObservableCollection<FriendUIModel> MyContacts { get; set; } = new ObservableCollection<FriendUIModel>();

    public AddFriendPage()
    {
        InitializeComponent();
        ContactsList.ItemsSource = MyContacts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        currentUserId = Preferences.Get("CurrentUserId", 0);
        await LoadContacts();
    }

    // VERİTABANINDAN GERÇEK KİŞİLERİ ÇEKME
    private async Task LoadContacts()
    {
        MyContacts.Clear();
        var requests = await _authService.GetPendingRequestsAsync(currentUserId);

        if (requests != null)
        {
            foreach (var req in requests)
            {
                // HATA 2 ÇÖZÜLDÜ: Senin modelindeki gerçek isimleri (SenderName, SenderPhone) kullandık
                MyContacts.Add(new FriendUIModel
                {
                    Name = string.IsNullOrEmpty(req.SenderName) ? "Bilinmeyen Kişi" : req.SenderName,
                    PhoneNumber = req.SenderPhone,
                    IsApproved = true // RequestModel'de onay durumu olmadığı için şimdilik hepsini onaylı (true) gösteriyoruz
                });
            }
        }
    }

    // YENİ KİŞİYE İSTEK GÖNDERME
    private async void OnSendRequestClicked(object sender, EventArgs e)
    {
        string phone = PhoneEntry.Text;
        if (string.IsNullOrWhiteSpace(phone))
        {
            await DisplayAlert("Hata", "Lütfen bir telefon numarası girin.", "Tamam");
            return;
        }

        bool success = await _authService.SendFriendRequestAsync(currentUserId, phone);
        if (success)
        {
            await DisplayAlert("Başarılı", "Acil durum kişisine istek gönderildi!", "Tamam");
            PhoneEntry.Text = "";
            await LoadContacts();
        }
        else
        {
            await DisplayAlert("Hata", "İstek gönderilemedi. Numaranın sisteme kayıtlı olduğundan emin olun.", "Tamam");
        }
    }

    // 📞 ARAMA BUTONU
    private void OnCallClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var phoneNumber = button.CommandParameter?.ToString();

        if (!string.IsNullOrEmpty(phoneNumber) && PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phoneNumber);
        }
    }
}

// Arayüz için yardımcı model (İsmini FriendUIModel yaptık)
public class FriendUIModel
{
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsApproved { get; set; }

    public string Initials => string.IsNullOrWhiteSpace(Name) || Name == "Bilinmeyen Kişi" ? "#" : Name.Substring(0, 1).ToUpper();
    public string StatusText => IsApproved ? "Onaylandı ✓" : "Beklemede ⏳";
    public Color StatusColor => IsApproved ? Colors.Green : Colors.Orange;
}