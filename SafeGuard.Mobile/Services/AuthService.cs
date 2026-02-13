using System.Text;
using System.Text.Json;
using SafeGuard.Mobile.Models; // Model klasörünü gördüğünden emin ol

namespace SafeGuard.Mobile.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        // Emülatör: 10.0.2.2
        private const string BaseUrl = "http://10.0.2.2:5161/api";

        public AuthService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        // Metodun dönüş tipini ve içini değiştiriyoruz
        // Dönüş tipi: (bool, int, string, string) oldu. 3. sıradaki "string" İsim için.
        public async Task<(bool IsSuccess, int UserId, string FullName, string ErrorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new { Email = email, Password = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/Users/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    // Backend'den gelen User nesnesini açıyoruz
                    var user = JsonSerializer.Deserialize<SafeGuard.Mobile.Models.User>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // BURASI ÖNEMLİ: user.FullName'i paketleyip geri yolluyoruz
                    return (true, user?.Id ?? 0, user?.FullName ?? "İsimsiz", null);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, 0, null, error);
            }
            catch (Exception ex)
            {
                return (false, 0, null, ex.Message);
            }
        }

        // RegisterAsync metodunun YENİ ve EKSİKSİZ hali
        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(User user)
        {
            try
            {
                // Backend'in beklediği tüm alanları User nesnesinden dolduruyoruz
                var registerData = new
                {
                    FullName = user.FullName,
                    Username = user.Email, // Username yerine Email gönderiyoruz (Backend uyumu için)
                    Email = user.Email,
                    Password = user.Password,
                    PhoneNumber = user.PhoneNumber,
                    // Arayüzden kaldırdığımız alanlara varsayılan değerler atıyoruz
                    BloodType = "Bilinmiyor",
                    ChronicDiseases = "Yok",
                    Allergies = "Yok",
                    Smoker = false,
                    AlcoholConsumption = false
                };

                var json = JsonSerializer.Serialize(registerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/Users/register", content);

                if (response.IsSuccessStatusCode)
                    return (true, null);

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Bağlantı hatası: {ex.Message}");
            }
        }

        public async Task<bool> SendSosAlertAsync(double latitude, double longitude)
        {
            await Task.Delay(500);
            return true;
        }
    }
}