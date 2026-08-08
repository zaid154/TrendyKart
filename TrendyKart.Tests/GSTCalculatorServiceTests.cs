using Xunit;
using TrendyKart.Services;
using TrendyKart.Models;

namespace TrendyKart.Tests
{
    public class GSTCalculatorServiceTests
    {
        private readonly GSTCalculatorService _gstService;

        public GSTCalculatorServiceTests()
        {
            _gstService = new GSTCalculatorService();
        }

        [Fact]
        public void CalculateGST_DefaultCategory_Returns18PercentRate()
        {
            var product = new Product { Price = 1000 };
            var rate = _gstService.GetEffectiveGSTPercentage(product);
            Assert.Equal(18m, rate);
        }

        [Fact]
        public void CalculateGST_SubCategoryOverride_TakesPrecedenceOverCategory()
        {
            var category = new Category { GSTPercentage = 18m };
            var subCategory = new SubCategory { Category = category, GSTPercentage = 12m };
            var product = new Product { Price = 1000, SubCategory = subCategory };

            var rate = _gstService.GetEffectiveGSTPercentage(product);
            Assert.Equal(12m, rate);
        }

        [Fact]
        public void CalculateGST_ProductOverride_TakesPrecedenceOverSubCategory()
        {
            var subCategory = new SubCategory { GSTPercentage = 12m };
            var product = new Product { Price = 1000, SubCategory = subCategory, GSTOverridePercentage = 5m };

            var rate = _gstService.GetEffectiveGSTPercentage(product);
            Assert.Equal(5m, rate);
        }

        [Fact]
        public void CalculateBasePriceAndGST_118Total_Returns100BaseAnd18GST()
        {
            var (basePrice, gstAmount, totalPrice) = _gstService.CalculateItemBreakup(118m, 18m, 1);

            Assert.Equal(100m, basePrice);
            Assert.Equal(18m, gstAmount);
            Assert.Equal(118m, totalPrice);
        }
    }
}
