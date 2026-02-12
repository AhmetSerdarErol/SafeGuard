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

        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(
            string username, string fullName, string email, string password,
            string phone, string blood, string diseases, string allergies, bool smoker, bool alcohol)
        {
            try
            {
                var registerData = new
                {
                    FullName = fullName,
                    Username = username,
                    Email = email,
                    Password = password,
                    PhoneNumber = phone,
                    BloodType = blood,
                    ChronicDiseases = diseases,
                    Allergies = allergies,
                    Smoker = smoker,
                    AlcoholConsumption = alcohol
                };

                var content = new StringContent(JsonSerializer.Serialize(registerData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/Users/register", content);

                return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<bool> SendSosAlertAsync(double latitude, double longitude)
        {
            await Task.Delay(500);
            return true;
        }
    }
}