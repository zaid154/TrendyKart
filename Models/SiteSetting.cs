using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class SiteSetting
    {
        [Key]
        public int SettingID { get; set; }

        [Required, StringLength(100)]
        public string StoreName { get; set; } = "TrendyKart";

        [StringLength(100)]
        public string ContactEmail { get; set; } = "support@trendykart.com";

        [StringLength(50)]
        public string ContactPhone { get; set; } = "+91 9876543210";

        public string Address { get; set; } = "123 Business Park, Tech Zone, New Delhi - 110001";

        [StringLength(500)]
        public string AuthorizedSignatureUrl { get; set; } = string.Empty;
    }
}
