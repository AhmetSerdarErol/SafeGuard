using System.Text.Json.Serialization;

namespace SafeGuard.Mobile.Models
{
    public class RequestModel
    {
        [JsonPropertyName("requestId")]
        public int Id { get; set; }

        [JsonPropertyName("requesterName")]
        public string SenderName { get; set; }

        [JsonPropertyName("requesterPhone")]
        public string SenderPhone { get; set; }

        [JsonPropertyName("requestDate")]
        public DateTime RequestDate { get; set; }
    }
}