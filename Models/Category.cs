using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTPercentage { get; set; } = 18.00m;

        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
