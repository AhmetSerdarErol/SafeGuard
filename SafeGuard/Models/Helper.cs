using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeGuard.Models
{
    public class Helper
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; } // İsteği Atan

        public int HelperId { get; set; }
        [ForeignKey("HelperId")]
        // HATA BURADAYDI: İsmi 'Helper' yerine 'HelperUser' yaptık
        public virtual User HelperUser { get; set; }

        public bool IsVerified { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}