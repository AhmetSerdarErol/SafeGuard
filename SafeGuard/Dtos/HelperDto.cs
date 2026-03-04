namespace SafeGuard.Dtos
{
    public class HelperDto
    {
        public int UserId { get; set; }          
        public string HelperPhoneNumber { get; set; } 
    }
    public class RespondDto
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
    }
}