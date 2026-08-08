using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class SubCategory
    {
        [Key]
        public int SubCategoryID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GSTPercentage { get; set; }

        public ICollection<CategoryFilterAttribute> FilterAttributes { get; set; } = new List<CategoryFilterAttribute>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
