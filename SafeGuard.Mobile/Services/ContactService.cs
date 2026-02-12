using SafeGuard.Mobile.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Contact = SafeGuard.Mobile.Models.Contact;

namespace SafeGuard.Mobile.Services
{
    public class ContactService
    {
        private readonly HttpClient _httpClient;
        // Emülatör kullanıyorsan 10.0.2.2, yoksa kendi IP'n
        private const string BaseUrl = "http://10.0.2.2:5161/api/contacts";

        public ContactService()
        {
            var handler = new HttpClientHandler();
            // Sertifika hatalarını yoksay (Geliştirme ortamı için)
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        // --- İŞTE BU METOD EKSİK OLABİLİR, O YÜZDEN HATA ALIYORDUN ---
        private async Task AddAuthorizationHeader()
        {
            // Token'ı güvenli depodan alıyoruz
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        // --- 1. KİŞİLERİ GETİR (ID İLE) ---
        public async Task<List<Contact>> GetContactsAsync(int userId)
        {
            await AddAuthorizationHeader(); // Token ekle
            try
            {
                // URL Sonuna ID ekle: api/contacts/5
                var response = await _httpClient.GetAsync($"{BaseUrl}/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Contact>>() ?? new List<Contact>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rehber Çekme Hatası: {ex.Message}");
            }
            return new List<Contact>();
        }

        // --- 2. KİŞİ EKLE ---
        public async Task<Contact> AddContactAsync(Contact contact)
        {
            await AddAuthorizationHeader(); // Token ekle
            try
            {
                // URL: api/contacts/add
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/add", contact);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Contact>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kişi Ekleme Hatası: {ex.Message}");
            }
            return null;
        }
    }
}