using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class ProductAttribute
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty; // Color, Storage, RAM, Size, Material, Processor

        public ICollection<AttributeValue> Values { get; set; } = new List<AttributeValue>();
    }

    public class AttributeValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttributeId { get; set; }

        [ForeignKey("AttributeId")]
        public ProductAttribute? Attribute { get; set; }

        [Required, StringLength(100)]
        public string Value { get; set; } = string.Empty; // 128GB, 256GB, Deep Purple, 16GB

        [StringLength(30)]
        public string? ColorHex { get; set; } // e.g. #4B384C for color swatches
    }

    public class ProductVariantAttribute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VariantId { get; set; }

        [ForeignKey("VariantId")]
        public ProductVariant? Variant { get; set; }

        [Required]
        public int AttributeValueId { get; set; }

        [ForeignKey("AttributeValueId")]
        public AttributeValue? AttributeValue { get; set; }
    }
}
