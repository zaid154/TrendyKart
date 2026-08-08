using System.ComponentModel.DataAnnotations;

namespace TrendyKart.Models
{
    public class ServiceablePincode
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(10)]
        public string Pincode { get; set; } = string.Empty;

        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        public int EstimatedDays { get; set; } = 3;

        public bool IsCODAvailable { get; set; } = true;

        public bool IsActive { get; set; } = true;
    }
}
