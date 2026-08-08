using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using TrendyKart.Services;

namespace TrendyKart.Tests
{
    public class RazorpayServiceTests
    {
        [Fact]
        public void VerifySignature_ValidSignature_ReturnsTrue()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Razorpay:KeyId"]).Returns("rzp_test_12345");
            configMock.Setup(c => c["Razorpay:KeySecret"]).Returns("test_secret_key_98765");

            var service = new RazorpayService(configMock.Object);

            string orderId = "order_9A33XWp2A654321";
            string paymentId = "pay_29ABcDEfGHIJKLM";
            
            // Expected HMAC-SHA256 hash string for orderId + "|" + paymentId using test_secret_key_98765
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("test_secret_key_98765"));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(orderId + "|" + paymentId));
            string validSignature = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            bool isValid = service.VerifySignature(orderId, paymentId, validSignature);
            Assert.True(isValid);
        }

        [Fact]
        public void VerifySignature_InvalidSignature_ReturnsFalse()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Razorpay:KeyId"]).Returns("rzp_test_12345");
            configMock.Setup(c => c["Razorpay:KeySecret"]).Returns("test_secret_key_98765");

            var service = new RazorpayService(configMock.Object);

            bool isValid = service.VerifySignature("order_123", "pay_456", "invalid_fake_signature");
            Assert.False(isValid);
        }
    }
}
