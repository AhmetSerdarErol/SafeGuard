namespace SafeGuard.Mobile.Models
{
    // Backend'e göndereceğimiz paket
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // Backend'den gelen cevap paketi
    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}