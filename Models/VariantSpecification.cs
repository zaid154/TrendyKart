using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class VariantSpecification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VariantId { get; set; }

        [ForeignKey("VariantId")]
        public ProductVariant? Variant { get; set; }

        [Required, StringLength(100)]
        public string SpecificationName { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string SpecificationValue { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }
}
