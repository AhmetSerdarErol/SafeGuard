using System.Text.Json.Serialization;

namespace SafeGuard.Mobile.Models
{
    public class RequestModel
    {
        // API "requestId" gönderiyor -> Biz "Id" olarak kullanıyoruz
        [JsonPropertyName("requestId")]
        public int Id { get; set; }

        // API "requesterName" gönderiyor -> Biz "SenderName" olarak kullanıyoruz
        [JsonPropertyName("requesterName")]
        public string SenderName { get; set; }

        // API "requesterPhone" gönderiyor -> Biz "SenderPhone" olarak kullanıyoruz
        // (Bunu eklemezsen numara görünmez!)
        [JsonPropertyName("requesterPhone")]
        public string SenderPhone { get; set; }

        [JsonPropertyName("requestDate")]
        public DateTime RequestDate { get; set; }
    }
}