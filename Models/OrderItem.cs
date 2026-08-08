using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }
        public Order? Order { get; set; }

        public int ProductID { get; set; }
        public Product? Product { get; set; }

        public int? VariantID { get; set; }
        [ForeignKey("VariantID")]
        public ProductVariant? Variant { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTAmount { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ItemTotal { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}