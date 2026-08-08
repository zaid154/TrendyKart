using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class CategoryFilterAttribute
    {
        [Key]
        public int AttributeID { get; set; }

        public int? CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }

        public int? SubCategoryID { get; set; }
        [ForeignKey("SubCategoryID")]
        public SubCategory? SubCategory { get; set; }

        [Required, StringLength(100)]
        public string AttributeName { get; set; } = string.Empty;

        [StringLength(50)]
        public string AttributeType { get; set; } = "Select";

        public string OptionsJson { get; set; } = "[]";
    }
}
