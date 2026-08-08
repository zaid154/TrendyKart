using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TrendyKart.Data;
using TrendyKart.Models;
using TrendyKart.Services;

namespace TrendyKart.Tests
{
    public class CouponServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ValidateCouponAsync_ExpiredCoupon_ReturnsInvalid()
        {
            using var db = GetInMemoryDbContext();
            db.Coupons.Add(new Coupon
            {
                Code = "EXPIRED50",
                DiscountType = "Percentage",
                DiscountValue = 50,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();

            var couponService = new CouponService(db);
            var result = await couponService.ValidateCouponAsync("EXPIRED50", 1000m, 1);

            Assert.False(result.IsValid);
            Assert.Contains("expired", result.ErrorMessage?.ToLower());
        }

        [Fact]
        public async Task ValidateCouponAsync_MinOrderAmountNotMet_ReturnsInvalid()
        {
            using var db = GetInMemoryDbContext();
            db.Coupons.Add(new Coupon
            {
                Code = "BIGBUY",
                DiscountType = "Flat",
                DiscountValue = 200,
                MinOrderAmount = 2000m,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(10)
            });
            await db.SaveChangesAsync();

            var couponService = new CouponService(db);
            var result = await couponService.ValidateCouponAsync("BIGBUY", 1500m, 1);

            Assert.False(result.IsValid);
            Assert.Contains("minimum", result.ErrorMessage?.ToLower());
        }

        [Fact]
        public async Task ValidateCouponAsync_ValidPercentageDiscount_CalculatesCorrectDiscount()
        {
            using var db = GetInMemoryDbContext();
            db.Coupons.Add(new Coupon
            {
                Code = "SAVE10",
                DiscountType = "Percentage",
                DiscountValue = 10,
                MaxDiscountCap = 500m,
                MinOrderAmount = 500m,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(10)
            });
            await db.SaveChangesAsync();

            var couponService = new CouponService(db);
            var result = await couponService.ValidateCouponAsync("SAVE10", 2000m, 1);

            Assert.True(result.IsValid);
            Assert.Equal(200m, result.DiscountAmount);
        }
    }
}
