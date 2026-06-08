using SafeGuard.Mobile.Models;
using System.Text.Json;

namespace SafeGuard.Mobile.Services
{
    public class ContactService
    {
        private readonly HttpClient _httpClient;


        private const string BaseUrl = "https://wql5wj50-5161.euw.devtunnels.ms/api/helpers";

        public ContactService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("X-Tunnel-Skip-AntiPhishing-Page", "true");
        }

        public async Task<List<ContactModel>> GetContactsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/contacts/{userId}");
                var contacts = JsonSerializer.Deserialize<List<ContactModel>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


                foreach (var c in contacts)
                {
                    Console.WriteLine($"--- DİKKAT: {c.Name} adlı kişinin SOS Durumu: {c.IsSosActive} ---");
                }

                return contacts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return new List<ContactModel>();
            }
        }
        public async Task<bool> DeleteContactAsync(int contactId)
        {
            try
            {
                int currentUserId = Preferences.Get("CurrentUserId", 0);
                string token = Preferences.Get("Token", "");

                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    string apiUrl = $"https://wql5wj50-7209.euw.devtunnels.ms/api/contacts/remove?userId={currentUserId}&contactId={contactId}";

                    var response = await client.DeleteAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"[SİLME HATASI] API'den Gelen: {errorContent}");

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Application.Current.MainPage.DisplayAlert("API Hatası", $"Gelen ID'ler: {errorContent}", "Tamam");
                        });

                        return false;
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Kişi silme servisi hatası: {ex.Message}");
                return false;
            }

        }
    }
} 