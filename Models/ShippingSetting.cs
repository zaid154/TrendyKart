using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class ShippingSetting
    {
        [Key]
        public int SettingID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FreeShippingThreshold { get; set; } = 500.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FlatShippingRate { get; set; } = 50.00m;

        [StringLength(200)]
        public string ShippingInfoText { get; set; } = "Free delivery on orders over ₹500";
    }
}
