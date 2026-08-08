using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        public int? VariantID { get; set; }

        [ForeignKey("VariantID")]
        public ProductVariant? Variant { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
