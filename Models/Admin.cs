using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [NotMapped]
        public string CurrentPassword { get; set; }
        [NotMapped]
        public string NewPassword { get; set; }
    }
}