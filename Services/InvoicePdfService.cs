using System;
using System.Text;
using System.Threading.Tasks;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public class InvoicePdfService : IInvoicePdfService
    {
        private readonly IEmailService _emailService;

        public InvoicePdfService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public string GenerateInvoiceHtml(Order order, SiteSetting siteSetting)
        {
            siteSetting ??= new SiteSetting();

            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>Invoice - " + order.OrderNumber + @"</title>
    <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; color: #333; }
        .invoice-box { max-width: 800px; margin: auto; padding: 30px; border: 1px solid #eee; box-shadow: 0 0 10px rgba(0, 0, 0, 0.05); font-size: 14px; line-height: 24px; border-radius: 8px; }
        .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #4361ee; padding-bottom: 20px; margin-bottom: 20px; }
        .store-name { font-size: 28px; font-weight: bold; color: #4361ee; margin: 0; }
        .invoice-title { font-size: 24px; font-weight: bold; text-align: right; color: #111; }
        .details-row { display: flex; justify-content: space-between; margin-bottom: 20px; }
        .table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        .table th { background: #f8f9fa; border-bottom: 2px solid #dee2e6; text-align: left; padding: 12px; }
        .table td { border-bottom: 1px solid #eee; padding: 12px; }
        .summary-box { margin-top: 20px; float: right; width: 300px; }
        .summary-row { display: flex; justify-content: space-between; padding: 6px 0; }
        .total-row { border-top: 2px solid #111; font-weight: bold; font-size: 16px; margin-top: 10px; padding-top: 10px; }
        .signature-section { margin-top: 60px; clear: both; text-align: right; }
        .signature-img { max-height: 70px; display: block; margin-left: auto; }
        @media print {
            .no-print { display: none; }
        }
    </style>
</head>
<body>
    <div class='no-print' style='text-align: right; max-width: 800px; margin: 0 auto 15px auto;'>
        <button onclick='window.print()' style='background: #4361ee; color: white; border: none; padding: 10px 20px; border-radius: 20px; font-weight: bold; cursor: pointer;'>Print / Save PDF</button>
    </div>

    <div class='invoice-box'>
        <div class='header'>
            <div>
                <h1 class='store-name'>" + (string.IsNullOrEmpty(siteSetting.StoreName) ? "TrendyKart" : siteSetting.StoreName) + @"</h1>
                <div style='color: #666; font-size: 12px; margin-top: 5px;'>" + siteSetting.Address + @"<br/>Email: " + siteSetting.ContactEmail + @" | Phone: " + siteSetting.ContactPhone + @"</div>
            </div>
            <div>
                <div class='invoice-title'>TAX INVOICE</div>
                <div style='color: #666;'>Invoice #: <strong>" + order.OrderNumber + @"</strong><br/>Date: " + order.OrderDate.ToString("dd MMM yyyy") + @"</div>
            </div>
        </div>

        <div class='details-row'>
            <div>
                <strong style='color: #4361ee;'>Billed To:</strong><br/>
                <strong>" + (order.Customer?.FullName ?? "Customer") + @"</strong><br/>
                " + order.ShippingAddress + @"<br/>
                " + order.City + @", " + order.State + @" - " + order.Pincode + @"<br/>
                Email: " + order.Email + @" | Phone: " + order.Phone + @"
            </div>
            <div style='text-align: right;'>
                <strong style='color: #4361ee;'>Payment Details:</strong><br/>
                Method: <strong>" + order.PaymentMethod + @"</strong><br/>
                Status: <strong>" + order.PaymentStatus + @"</strong><br/>
                Razorpay ID: " + (order.RazorpayPaymentId ?? "N/A") + @"
            </div>
        </div>

        <table class='table'>
            <thead>
                <tr>
                    <th>Item Description</th>
                    <th>Variant</th>
                    <th style='text-align: right;'>Base Price</th>
                    <th style='text-align: right;'>GST %</th>
                    <th style='text-align: right;'>GST Amt</th>
                    <th style='text-align: center;'>Qty</th>
                    <th style='text-align: right;'>Total</th>
                </tr>
            </thead>
            <tbody>");

            foreach (var item in order.OrderItems)
            {
                sb.Append(@"
                <tr>
                    <td><strong>" + item.ProductName + @"</strong></td>
                    <td>" + (string.IsNullOrEmpty(item.VariantName) ? "-" : item.VariantName) + @"</td>
                    <td style='text-align: right;'>₹" + item.Price + @"</td>
                    <td style='text-align: right;'>" + item.GSTPercentage + @"%</td>
                    <td style='text-align: right;'>₹" + item.GSTAmount + @"</td>
                    <td style='text-align: center;'>" + item.Quantity + @"</td>
                    <td style='text-align: right;'>₹" + item.ItemTotal + @"</td>
                </tr>");
            }

            sb.Append(@"
            </tbody>
        </table>

        <div class='summary-box'>
            <div class='summary-row'><span>Subtotal (Excl. Tax):</span><span>₹" + order.SubTotal + @"</span></div>
            <div class='summary-row'><span>Total GST:</span><span>₹" + order.GSTTotal + @"</span></div>
            <div class='summary-row'><span>Shipping Charge:</span><span>" + (order.ShippingCharge == 0 ? "FREE" : "₹" + order.ShippingCharge) + @"</span></div>");

            if (order.DiscountAmount > 0)
            {
                sb.Append(@"<div class='summary-row' style='color: green;'><span>Coupon Discount (" + order.CouponCode + @"):</span><span>-₹" + order.DiscountAmount + @"</span></div>");
            }

            sb.Append(@"
            <div class='summary-row total-row'><span>Grand Total:</span><span>₹" + order.TotalAmount + @"</span></div>
        </div>

        <div class='signature-section'>
            " + (!string.IsNullOrEmpty(siteSetting.AuthorizedSignatureUrl) ? "<img src='" + siteSetting.AuthorizedSignatureUrl + "' class='signature-img' alt='Authorized Signature'/>" : "<div style='height: 50px;'></div>") + @"
            <div style='font-weight: bold; margin-top: 5px;'>Authorized Signatory</div>
            <div style='color: #888; font-size: 11px;'>" + siteSetting.StoreName + @" Digital Stamp</div>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        public async Task SendInvoiceEmailAsync(Order order, SiteSetting siteSetting)
        {
            string htmlContent = GenerateInvoiceHtml(order, siteSetting);
            string subject = $"Order Invoice - #{order.OrderNumber} - {siteSetting.StoreName}";
            await _emailService.SendEmailAsync(order.Email, subject, htmlContent);
        }
    }
}
