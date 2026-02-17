namespace SafeGuard.Dtos
{
    public class HelperDto
    {
        public int UserId { get; set; }          // İsteyen Kişi (Sen)
        public string HelperPhoneNumber { get; set; } // Eklemek İstediğin Kişinin Numarası
    }

    public class RespondDto
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
    }
}