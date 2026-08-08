using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        [StringLength(100)]
        public string VariantName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }

        public int Stock { get; set; } = 0;
        public int ReservedStock { get; set; } = 0;

        [NotMapped]
        public int AvailableStock => Math.Max(0, Stock - ReservedStock);

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; } // in kg

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Length { get; set; } // in cm

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Width { get; set; } // in cm

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Height { get; set; } // in cm

        [StringLength(50)]
        public string? ColorName { get; set; }

        [StringLength(30)]
        public string? ColorHex { get; set; }

        [StringLength(50)]
        public string? Storage { get; set; } // e.g. 128GB, 256GB, 512GB, 1TB

        [StringLength(50)]
        public string? RAM { get; set; } // e.g. 8GB, 12GB, 16GB

        [StringLength(100)]
        public string? Processor { get; set; } // e.g. A19 Pro, Snapdragon 8 Gen 3

        [StringLength(100)]
        public string? ModelNumber { get; set; }

        [StringLength(150)]
        public string? Warranty { get; set; }

        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public string? SpecificationsJson { get; set; }
        public string? AttributesJson { get; set; }

        public ICollection<ProductMedia> MediaList { get; set; } = new List<ProductMedia>();
        public ICollection<VariantSpecification> Specifications { get; set; } = new List<VariantSpecification>();
        public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = new List<ProductVariantAttribute>();
    }
}
