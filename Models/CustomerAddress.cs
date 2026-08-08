using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class CustomerAddress
    {
        [Key]
        public int AddressID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        [Required, StringLength(50)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string Pincode { get; set; } = string.Empty;

        [StringLength(20)]
        public string AddressType { get; set; } = "Home"; // Home, Office, Other

        public bool IsDefault { get; set; } = false;
    }
}
