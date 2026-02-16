namespace SafeGuard.Models
{
    public class Helper
    {
        public int Id { get; set; }

        public int UserId { get; set; } // İsteği Gönderen Kişi
        public User User { get; set; }  // İlişki (Adını görmek için lazım)

        public int HelperId { get; set; } // İstek Gönderilen (Hedef) Kişinin ID'si

        public bool IsVerified { get; set; } = false; // Onaylandı mı?
        public string Status { get; set; } = "Pending"; // Durum: Pending, Accepted, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}