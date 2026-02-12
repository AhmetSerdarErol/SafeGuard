namespace SafeGuard.Mobile.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public int UserId { get; set; }

        // YENİ: Varsayılan olarak "Pending" (Bekliyor) gelir
        public string VerificationStatus { get; set; } = "Pending";
    }
}