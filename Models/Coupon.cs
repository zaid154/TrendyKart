using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Coupon
    {
        [Key]
        public int CouponID { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string DiscountType { get; set; } = "Flat";

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountCap { get; set; }

        [StringLength(30)]
        public string UsageType { get; set; } = "EveryOrder";

        public int? TotalUsageLimit { get; set; }
        public int? PerUserUsageLimit { get; set; }
        public int TimesUsed { get; set; } = 0;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
