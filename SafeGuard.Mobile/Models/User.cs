namespace SafeGuard.Mobile.Models
{
    public class User
    {
        // Temel Bilgiler
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public int Id { get; set; }
        public string BloodType { get; set; }
        public string ChronicDiseases { get; set; }
        public string Allergies { get; set; }
        public string Surgeries { get; set; }
        public string Habits { get; set; }
    }
}