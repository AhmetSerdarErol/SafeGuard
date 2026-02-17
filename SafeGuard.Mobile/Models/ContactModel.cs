using System.Text.Json.Serialization;

namespace SafeGuard.Mobile.Models
{
    public class ContactModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
}