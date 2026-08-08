using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class ProductMedia
    {
        [Key]
        public int MediaID { get; set; }

        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        public int? VariantID { get; set; }

        [ForeignKey("VariantID")]
        public ProductVariant? Variant { get; set; }

        [Required, StringLength(20)]
        public string MediaType { get; set; } = "Image";

        [Required, StringLength(500)]
        public string MediaUrl { get; set; } = string.Empty;

        public long FileSize { get; set; } = 0;

        public int SortOrder { get; set; } = 0;
    }
}
