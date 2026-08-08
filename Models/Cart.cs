using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Cart
    {
        [Key]
        public int CartID { get; set; }

        public int CustomerID { get; set; }
        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        public int ProductID { get; set; }
        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        public int? VariantID { get; set; }
        [ForeignKey("VariantID")]
        public ProductVariant? Variant { get; set; }

        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}