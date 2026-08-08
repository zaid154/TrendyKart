using TrendyKart.Models;

namespace TrendyKart.Services
{
    public interface IGSTCalculatorService
    {
        decimal GetEffectiveGSTPercentage(Product product);
        (decimal BasePrice, decimal GSTAmount, decimal TotalPrice) CalculateItemBreakup(decimal price, decimal gstPercentage, int quantity);
    }
}
