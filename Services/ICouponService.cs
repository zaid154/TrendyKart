using System.Threading.Tasks;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Coupon? Coupon { get; set; }
    }

    public interface ICouponService
    {
        Task<CouponValidationResult> ValidateCouponAsync(string code, decimal orderSubTotal, int customerId);
    }
}
