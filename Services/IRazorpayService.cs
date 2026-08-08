namespace TrendyKart.Services
{
    public interface IRazorpayService
    {
        string CreateRazorpayOrder(string orderNumber, decimal amountInINR);
        bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string signature);
    }
}
