using System.ComponentModel.DataAnnotations;
namespace TrendyKart.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        // OTP
        public string? OTP { get; set; }
        public DateTime? OTPExpiry { get; set; }
        public int OTPAttempts { get; set; } = 0;
        // Default Address
        public string? DefaultStreet { get; set; }
        public string? DefaultCity { get; set; }
        public string? DefaultState { get; set; }
        public string? DefaultPincode { get; set; }
        public string? DefaultCountry { get; set; }
        public ICollection<Order>? Orders { get; set; }
    }
}