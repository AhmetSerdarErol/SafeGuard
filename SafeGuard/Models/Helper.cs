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
        public virtual User User { get; set; }
        public int HelperId { get; set; }
        [ForeignKey("HelperId")]
        public virtual User HelperUser { get; set; }
        public bool IsVerified { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool UserAllowsHelperToView { get; set; } = false;
        public bool HelperAllowsUserToView { get; set; } = false;
    }
}