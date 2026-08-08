using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(string code, decimal orderSubTotal, int customerId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Please enter a coupon code." };

            string cleanCode = code.Trim().ToUpper();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cleanCode);

            if (coupon == null || !coupon.IsActive)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Invalid or expired coupon code." };

            if (coupon.StartDate.HasValue && DateTime.UtcNow < coupon.StartDate.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Coupon is not active yet." };

            if (coupon.EndDate.HasValue && DateTime.UtcNow > coupon.EndDate.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Coupon has expired." };

            if (orderSubTotal < coupon.MinOrderAmount)
                return new CouponValidationResult { IsValid = false, ErrorMessage = $"Minimum order amount of ₹{coupon.MinOrderAmount} required for this coupon." };

            if (coupon.TotalUsageLimit.HasValue && coupon.TimesUsed >= coupon.TotalUsageLimit.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Coupon usage limit reached." };

            if (coupon.UsageType == "FirstOrderOnly")
            {
                bool hasPreviousOrders = await _context.Orders.AnyAsync(o => o.CustomerID == customerId && o.Status != "Cancelled");
                if (hasPreviousOrders)
                    return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon is valid for your first order only." };
            }

            if (coupon.PerUserUsageLimit.HasValue)
            {
                int userTimesUsed = await _context.Orders.CountAsync(o => o.CustomerID == customerId && o.CouponCode == cleanCode && o.Status != "Cancelled");
                if (userTimesUsed >= coupon.PerUserUsageLimit.Value)
                    return new CouponValidationResult { IsValid = false, ErrorMessage = "You have reached the maximum usage limit for this coupon." };
            }

            decimal discount = 0;
            if (coupon.DiscountType == "Percentage")
            {
                discount = Math.Round(orderSubTotal * (coupon.DiscountValue / 100m), 2);
                if (coupon.MaxDiscountCap.HasValue && discount > coupon.MaxDiscountCap.Value)
                {
                    discount = coupon.MaxDiscountCap.Value;
                }
            }
            else
            {
                discount = coupon.DiscountValue;
            }

            if (discount > orderSubTotal) discount = orderSubTotal;

            return new CouponValidationResult
            {
                IsValid = true,
                DiscountAmount = discount,
                Coupon = coupon
            };
        }
    }
}
