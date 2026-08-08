using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TrendyKart.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        public string Category { get; set; } = string.Empty;

        public int? CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public Category? CategoryRef { get; set; }

        public int? SubCategoryID { get; set; }
        [ForeignKey("SubCategoryID")]
        public SubCategory? SubCategory { get; set; }

        [Display(Name = "Brand")]
        [StringLength(50)]
        public string? Brand { get; set; }

        // Common Thumbnail
        public string ImageUrl { get; set; } = string.Empty;

        // Flags & Status
        public bool IsFeatured { get; set; }
        public bool IsBestseller { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        // Display Ratings & Reviews
        [Range(0, 5)]
        public double Rating { get; set; } = 4.5;
        public int TotalReviews { get; set; } = 12;

        public bool FreeShipping { get; set; } = true;
        public string? DeliveryInfo { get; set; }
        public string? Tags { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GSTOverridePercentage { get; set; }

        // Deprecated fields kept for database compatibility if needed, mapped to default variant
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }
        public int Stock { get; set; }
        public string? SKU { get; set; }
        public string? SpecificationsJson { get; set; }
        public string? AvailableSizes { get; set; }
        public string? AvailableColors { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductMedia> MediaFiles { get; set; } = new List<ProductMedia>();
    }
}