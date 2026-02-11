using System.Text;
using System.Text.Json;
using SafeGuard.Mobile.Models;

namespace SafeGuard.Mobile.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        // Backend adresi (Tek yerden yönetelim)
        // NOT: Android Emülatör için 10.0.2.2 kullanılır. Gerçek cihazda bilgisayarın IP'si gerekir.
        private const string BaseUrl = "http://10.0.2.2:5161/api/Users";

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        // --- GİRİŞ YAP (LOGIN) ---
        public async Task<(bool IsSuccess, string ErrorMessage)> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new LoginRequest { Email = email, Password = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // URL'i dinamik yaptık: BaseUrl + "/login"
                var response = await _httpClient.PostAsync($"{BaseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var token = await response.Content.ReadAsStringAsync();

                    // Token ve E-posta'yı güvenli hafızaya atıyoruz
                    await SecureStorage.Default.SetAsync("auth_token", token);
                    await SecureStorage.Default.SetAsync("user_email", email);

                    return (true, null);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Giriş Başarısız: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Bağlantı Hatası: {ex.Message}");
            }
        }

        // --- KAYIT OL (REGISTER) - YENİ EKLENDİ ---
        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(string username, string email, string password)
        {
            try
            {
                // Backend'in beklediği veri formatı (UserRegisterDto)
                var registerData = new
                {
                    Username = username,
                    Email = email,
                    Password = password
                };

                var json = JsonSerializer.Serialize(registerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // İstek Adresi: http://10.0.2.2:5161/api/Users/register
                var response = await _httpClient.PostAsync($"{BaseUrl}/register", content);

                if (response.IsSuccessStatusCode)
                {
                    // Kayıt başarılıysa true dön
                    return (true, null);
                }
                else
                {
                    // Backend'den gelen hatayı (örn: "Bu e-posta zaten kayıtlı") oku
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Kayıt Başarısız: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Bağlantı Hatası: {ex.Message}");
            }
        }
    }
}