using SafeGuard.Mobile.Models;
using System.Net.Http.Headers; 
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SafeGuard.Mobile.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        
        private const string BaseUrl = "https://wql5wj50-7209.euw.devtunnels.ms";

        public AuthService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);

            _httpClient.DefaultRequestHeaders.Add("X-Tunnel-Skip-AntiPhishing-Page", "true");
        }

        
        private void AttachBearerToken()
        {
            var token = Preferences.Get("Token", string.Empty);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<(bool IsSuccess, int UserId, string FullName, string ErrorMessage)> LoginAsync(string email, string password)
        {
            string url = $"{BaseUrl}/api/Users/login";

            try
            {
                var loginData = new { Email = email.Trim(), Password = password.Trim() };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseData = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    SafeGuard.Mobile.Models.User user = null;

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(responseData);

                        if (doc.RootElement.TryGetProperty("token", out JsonElement tokenEl) || doc.RootElement.TryGetProperty("Token", out tokenEl))
                        {
                            Preferences.Set("Token", tokenEl.GetString() ?? "");
                        }

                        if (doc.RootElement.TryGetProperty("user", out JsonElement userEl) || doc.RootElement.TryGetProperty("User", out userEl))
                        {
                            user = JsonSerializer.Deserialize<SafeGuard.Mobile.Models.User>(userEl.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        return (false, 0, null, $"JSON Okuma Hatası: {jsonEx.Message} \n\nGelen Veri: {responseData}");
                    }

                    if (user != null)
                    {
                        Preferences.Set("CurrentUserId", user.Id);
                        Preferences.Set("UserFullName", user.FullName ?? "");
                        Preferences.Set("UserPhone", user.PhoneNumber ?? "");
                        Preferences.Set("UserBlood", user.BloodType ?? "");
                        
                        var fcmToken = Preferences.Get("FcmToken", "");
                        if (!string.IsNullOrEmpty(fcmToken))
                        {
                            await UpdateFcmTokenAsync(user.Id, fcmToken);
                        }
                        return (true, user.Id, user.FullName ?? "İsimsiz", null);
                    }
                    else
                    {
                        return (false, 0, null, $"Kullanıcı nesnesi boş! \n\nGelen Veri: {responseData}");
                    }
                }

                return (false, 0, null, $"Sunucu Hatası: {(int)response.StatusCode} \nDetay: {responseData}");
            }
            catch (Exception ex)
            {
                return (false, 0, null, $"Bağlantı Hatası: {ex.Message}");
            }
        }

        public async Task<bool> RegisterAsync(UserRegisterDto userDto)
        {
            try { return (await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/Users/register", userDto)).IsSuccessStatusCode; }
            catch { return false; }
        }

        public async Task<bool> UpdateFcmTokenAsync(int userId, string token)
        {
            try
            {
                AttachBearerToken(); 
                return (await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/Users/update-fcm-token", new { UserId = userId, Token = token })).IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateTokenAsync(int userId, string token) => await UpdateFcmTokenAsync(userId, token);

        public async Task<bool> SendSosAlertAsync(int userId, double latitude, double longitude)
        {
            try
            {
                
                string userToken = Preferences.Get("Token", "");
                
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/Sos/send", new { userId = userId, latitude = latitude, longitude = longitude });

               
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("GERÇEK HATA RAPORU", $"Kod: {(int)response.StatusCode}\nSebep: {errorContent}", "Tamam");
                });

                return false;
            }
            catch (Exception ex)
            {
                
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("SİSTEM ÇÖKTÜ", ex.Message, "Tamam");
                });
                return false;
            }
        }

        public async Task<bool> SendFriendRequestAsync(int myUserId, string targetPhone)
        {
            try
            {
                AttachBearerToken(); 
                return (await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/Helpers/add", new { UserId = myUserId, HelperPhoneNumber = targetPhone })).IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<RequestModel>> GetPendingRequestsAsync(int myUserId)
        {
            try
            {
                AttachBearerToken(); 
                var response = await _httpClient.GetFromJsonAsync<List<RequestModel>>($"{BaseUrl}/api/Helpers/requests/{myUserId}");
                return response ?? new List<RequestModel>();
            }
            catch { return new List<RequestModel>(); }
        }

        public async Task<bool> RespondToRequestAsync(int requestId, bool accept)
        {
            try
            {
                AttachBearerToken(); 
                return (await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/Helpers/respond", new { RequestId = requestId, Accept = accept })).IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<List<ContactModel>> GetContactsAsync(int userId)
        {
            try
            {
                AttachBearerToken();
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/Helpers/contacts/{userId}");

                var rawJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var result = JsonSerializer.Deserialize<List<ContactModel>>(rawJson, options);
                        return result ?? new List<ContactModel>();
                    }
                    catch (Exception jsonEx)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            Application.Current.MainPage.DisplayAlert("JSON Hatası", $"Model uyuşmazlığı: {jsonEx.Message}", "Tamam"));
                        return new List<ContactModel>();
                    }
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        Application.Current.MainPage.DisplayAlert("API Hatası", $"Hata Kodu: {response.StatusCode}", "Tamam"));
                    return new List<ContactModel>();
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    Application.Current.MainPage.DisplayAlert("Bağlantı Hatası", ex.Message, "Tamam"));
                return new List<ContactModel>();
            }
        }
        public async Task<bool> CheckMySosStatusAsync(int userId)
        {
            try
            {
                string token = Preferences.Get("Token", "");
                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    string apiUrl = $"https://wql5wj50-7209.euw.devtunnels.ms/api/sos/CheckStatus?userId={userId}";

                    var response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var resultString = await response.Content.ReadAsStringAsync();

                        if (bool.TryParse(resultString, out bool isSosActive))
                        {
                            return isSosActive;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SOS Durum Kontrolü Hatası: {ex.Message}");
            }

            return false;
        }
        
        public async Task<string> UploadProfilePhotoAsync(int userId, FileResult fileResult)
        {
            try
            {
                AttachBearerToken(); 
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(await fileResult.OpenReadAsync()), "file", fileResult.FileName);
                return (await _httpClient.PostAsync($"{BaseUrl}/api/Users/upload-photo/{userId}", content)).IsSuccessStatusCode ? "OK" : null;
            }
            catch { return null; }
        }
        public async Task<bool> CancelSosAsync(int userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/api/Users/cancel-sos/{userId}", null);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Güvendeyim bildirimi gönderilemedi: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateFullProfileInfoAsync(int userId, string name, string phone, string height, string weight, string blood, string conditions, string allergies, string meds, string organStatus, string organDetails, string alcohol, string smoking)
        {
            try
            {
                AttachBearerToken();
                var data = new { Id = userId, FullName = name, PhoneNumber = phone, Height = string.IsNullOrEmpty(height) ? (int?)null : int.Parse(height), Weight = string.IsNullOrEmpty(weight) ? (int?)null : int.Parse(weight), BloodType = blood, MedicalConditions = conditions, Allergies = allergies, Medications = meds, OrganStatus = organStatus, OrganDetails = organDetails, AlcoholUse = alcohol, SmokingHabit = smoking };
                return (await _httpClient.PutAsJsonAsync($"{BaseUrl}/api/Users/update-info/{userId}", data)).IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}