using SafeGuard.Mobile.Models;
using System.Text.Json;

namespace SafeGuard.Mobile.Services
{
    public class ContactService
    {
        private readonly HttpClient _httpClient;

        // ESKİ HALİNE GERİ DÖNDÜRDÜK
        private const string BaseUrl = "http://172.16.0.78:5161/api/helpers";

        public ContactService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        public async Task<List<ContactModel>> GetContactsAsync(int userId)
        {
            try
            {
                // ESKİ HALİNE GERİ DÖNDÜRDÜK
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/contacts/{userId}");

                return JsonSerializer.Deserialize<List<ContactModel>>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return new List<ContactModel>();
            }
        }
    }
}