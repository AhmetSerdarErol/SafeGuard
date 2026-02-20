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
        private const string BaseUrl = "http://172.16.0.78:5161/api";

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