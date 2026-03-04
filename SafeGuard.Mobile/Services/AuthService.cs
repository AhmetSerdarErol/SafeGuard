using SafeGuard.Mobile.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SafeGuard.Mobile.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        // 🟢 EMÜLATÖR İÇİN SABİT IP (Değiştirme)
        private const string BaseUrl = "http://172.16.0.38:5161/api";

        public AuthService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

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
                    var user = JsonSerializer.Deserialize<SafeGuard.Mobile.Models.User>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // 🌟 İŞTE KRİTİK KISIM: Profil sayfanın aradığı İSİMLERLE BİREBİR AYNI şekilde kaydediyoruz!
                    Preferences.Set("CurrentUserId", user?.Id ?? 0);
                    Preferences.Set("UserFullName", user?.FullName ?? "");
                    Preferences.Set("UserPhone", user?.PhoneNumber ?? "");
                    Preferences.Set("UserHeight", user?.Height?.ToString() ?? "");
                    Preferences.Set("UserWeight", user?.Weight?.ToString() ?? "");
                    Preferences.Set("UserBlood", user?.BloodType ?? "");
                    Preferences.Set("UserConditions", user?.MedicalConditions ?? "");
                    Preferences.Set("UserAllergies", user?.Allergies ?? "");
                    Preferences.Set("UserMedications", user?.Medications ?? "");
                    Preferences.Set("UserOrganStatus", string.IsNullOrEmpty(user?.OrganStatus) ? "Yok" : user?.OrganStatus);
                    Preferences.Set("UserOrganDetails", user?.OrganDetails ?? "");
                    Preferences.Set("UserAlcohol", user?.AlcoholUse ?? "");
                    Preferences.Set("UserSmoking", user?.SmokingHabit ?? "");

                    return (true, user?.Id ?? 0, user?.FullName ?? "İsimsiz", null);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, 0, null, error);
            }
            catch (Exception ex) { return (false, 0, null, ex.Message); }
        }

        // 🟢 YENİ VE DÜZELTİLMİŞ REGISTER METODU (DTO kullanıyor)
        public async Task<bool> RegisterAsync(UserRegisterDto userDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/users/register", userDto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
        public async Task<bool> UpdateFcmTokenAsync(int userId, string token)
        {
            try
            {
                var payload = new { UserId = userId, Token = token };
                string json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Senin güncel IP adresini (172.16.0.38) buraya yazdım. 
                // Eğer backend'de metodu AuthController içine yazdıysan adres böyle kalmalı.
                // Eğer UsersController içine yazdıysan "api/Users/update-fcm-token" yapmalısın.
                var response = await _httpClient.PostAsync("http://172.16.0.38:5161/api/Users/update-fcm-token", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token gönderme hatası: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateTokenAsync(int userId, string token)
        {
            try
            {
                // API'nin adresini kendi API URL'in ile değiştir (Örn: http://10.0.2.2:5161 veya gerçek IP)
                string apiUrl = $"http://172.16.0.38:5161/api/Users/update-fcm-token";

                var data = new { UserId = userId, Token = token };
                string json = System.Text.Json.JsonSerializer.Serialize(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // HttpClient ile API'ye gönderiyoruz
                using var client = new HttpClient();
                var response = await client.PostAsync(apiUrl, content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> SendSosAlertAsync(int senderId, int targetUserId)
        {
            try
            {
                // Backend'deki füze kapımızın adresi (IP'nin aynı kaldığını varsayıyoruz, değiştiyse güncellersin)
                string url = $"http://172.16.0.38:5161/api/Users/send-sos?senderId={senderId}&targetUserId={targetUserId}";

                // Post isteğini atıyoruz (İçine data koymuyoruz çünkü ID'leri URL'den gönderdik)
                var response = await _httpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    return true; // Füze başarıyla ateşlendi!
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Füze Hatası: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bağlantı Hatası: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> SendFriendRequestAsync(int myUserId, string targetPhone)
        {
            try
            {
                var payload = new { UserId = myUserId, HelperPhoneNumber = targetPhone };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/helpers/add", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> SendSosAlertAsync(double latitude, double longitude)
        {
            // Backend'e konum göndermek istersen burayı doldurabilirsin
            await Task.Delay(500);
            return true;
        }

        public async Task<List<RequestModel>> GetPendingRequestsAsync(int myUserId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/helpers/requests/{myUserId}");
                return JsonSerializer.Deserialize<List<RequestModel>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return new List<RequestModel>(); }
        }

        public async Task<bool> RespondToRequestAsync(int requestId, bool accept)
        {
            try
            {
                var payload = new { RequestId = requestId, Accept = accept };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/helpers/respond", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<string> UploadProfilePhotoAsync(int userId, FileResult fileResult)
        {
            try
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(await fileResult.OpenReadAsync()), "file", fileResult.FileName);

                var response = await _httpClient.PostAsync($"{BaseUrl}/users/upload-photo/{userId}", content);
                if (response.IsSuccessStatusCode) return "OK";
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> UpdateFullProfileInfoAsync(
            int userId, string name, string phone, string height, string weight,
            string blood, string conditions, string allergies, string meds,
            string organStatus, string organDetails, string alcohol, string smoking)
        {
            try
            {
                var data = new
                {
                    Id = userId,
                    FullName = name,
                    PhoneNumber = phone,
                    Height = string.IsNullOrEmpty(height) ? (int?)null : int.Parse(height),
                    Weight = string.IsNullOrEmpty(weight) ? (int?)null : int.Parse(weight),
                    BloodType = blood,
                    MedicalConditions = conditions,
                    Allergies = allergies,
                    Medications = meds,
                    OrganStatus = organStatus,
                    OrganDetails = organDetails,
                    AlcoholUse = alcohol,
                    SmokingHabit = smoking
                };

                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/users/update-info/{userId}", data);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}