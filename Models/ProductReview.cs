using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(100)]
        public string Headline { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
