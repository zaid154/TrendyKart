using System;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public class GSTCalculatorService : IGSTCalculatorService
    {
        public decimal GetEffectiveGSTPercentage(Product product)
        {
            if (product == null) return 18.00m;

            if (product.GSTOverridePercentage.HasValue)
                return product.GSTOverridePercentage.Value;

            if (product.SubCategory != null && product.SubCategory.GSTPercentage.HasValue)
                return product.SubCategory.GSTPercentage.Value;

            if (product.SubCategory != null && product.SubCategory.Category != null)
                return product.SubCategory.Category.GSTPercentage;

            if (product.CategoryRef != null)
                return product.CategoryRef.GSTPercentage;

            return 18.00m;
        }

        public (decimal BasePrice, decimal GSTAmount, decimal TotalPrice) CalculateItemBreakup(decimal price, decimal gstPercentage, int quantity)
        {
            decimal totalPrice = price * quantity;
            decimal basePrice = Math.Round(totalPrice / (1 + (gstPercentage / 100m)), 2);
            decimal gstAmount = totalPrice - basePrice;
            return (basePrice, gstAmount, totalPrice);
        }
    }
}
