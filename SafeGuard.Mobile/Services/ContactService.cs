using SafeGuard.Mobile.Models;
using System.Text.Json;

namespace SafeGuard.Mobile.Services
{
    public class ContactService
    {
        private readonly HttpClient _httpClient;

        // Emülatör IP adresi (Backend ile aynı port olduğundan emin ol)
        private const string BaseUrl = "http://10.0.2.2:5161/api/helpers";

        public ContactService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        // Metodun 'List<ContactModel>' döndürdüğüne dikkat et
        public async Task<List<ContactModel>> GetContactsAsync(int userId)
        {
            try
            {
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