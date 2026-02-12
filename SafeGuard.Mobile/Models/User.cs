namespace SafeGuard.Mobile.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Diğer alanlar login cevabı için şimdilik şart değil
    }
}