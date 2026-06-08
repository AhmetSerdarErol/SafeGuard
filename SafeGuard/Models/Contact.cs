namespace SafeGuard.API.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
    }
}