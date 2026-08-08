using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TrendyKart.Data;
using TrendyKart.Models;
using TrendyKart.Services;

namespace TrendyKart.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICouponService _couponService;
        private readonly IGSTCalculatorService _gstCalculator;
        private readonly IInvoicePdfService _invoiceService;
        private readonly IRazorpayService _razorpayService;

        public OrderController(
            ApplicationDbContext context,
            ICouponService couponService,
            IGSTCalculatorService gstCalculator,
            IInvoicePdfService invoiceService,
            IRazorpayService razorpayService)
        {
            _context = context;
            _couponService = couponService;
            _gstCalculator = gstCalculator;
            _invoiceService = invoiceService;
            _razorpayService = razorpayService;
        }

        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code, decimal subTotal)
        {
            int customerId = GetCurrentCustomerId();
            var result = await _couponService.ValidateCouponAsync(code, subTotal, customerId);
            return Json(new
            {
                isValid = result.IsValid,
                discountAmount = result.DiscountAmount,
                errorMessage = result.ErrorMessage,
                code = result.Coupon?.Code
            });
        }

        public async Task<IActionResult> OrderHistory()
        {
            int customerId = GetCurrentCustomerId();
            var orders = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderFeedbacks)
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderID == id && (o.CustomerID == customerId || User.IsInRole("Admin")));

            if (order == null)
                return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderID == id && (o.CustomerID == customerId || User.IsInRole("Admin")));

            if (order == null)
                return NotFound();

            var siteSetting = await _context.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();
            string html = _invoiceService.GenerateInvoiceHtml(order, siteSetting);
            return Content(html, "text/html");
        }

        [HttpPost]
        public async Task<IActionResult> ResendInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null) return NotFound();

            var siteSetting = await _context.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();
            await _invoiceService.SendInvoiceEmailAsync(order, siteSetting);
            TempData["SuccessMessage"] = "Invoice email resent successfully.";
            return RedirectToAction("Orders", "Admin");
        }

        public async Task<IActionResult> GiveFeedback(int id)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null || order.Status != "Delivered")
                return NotFound();

            var existingFeedback = await _context.OrderFeedbacks
                .FirstOrDefaultAsync(f => f.OrderId == id && f.CustomerId == customerId);
            if (existingFeedback != null)
            {
                return RedirectToAction("ViewFeedback", new { id = id });
            }
            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int orderId, int rating, string comment)
        {
            int customerId = GetCurrentCustomerId();
            var feedback = await _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.OrderId == orderId && f.CustomerId == customerId);

            if (feedback == null)
            {
                feedback = new OrderFeedback
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    Rating = rating,
                    IsReadByAdmin = false,
                    CreatedAt = DateTime.UtcNow,
                    Messages = new System.Collections.Generic.List<FeedbackMessage>()
                };
                _context.OrderFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();
            }

            var newMessage = new FeedbackMessage
            {
                FeedbackId = feedback.Id,
                SenderRole = "Customer",
                Message = comment,
                SentAt = DateTime.UtcNow
            };
            _context.FeedbackMessages.Add(newMessage);
            feedback.IsReadByAdmin = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message sent successfully.";
            return RedirectToAction("ViewFeedback", new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null)
                return NotFound();

            if (order.Status != "Order Placed" && order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Order cannot be cancelled after processing/shipping.";
                return RedirectToAction(nameof(OrderHistory));
            }

            order.Status = "Cancelled";
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductID);
                    if (product != null) product.Stock += item.Quantity;
                    if (item.VariantID.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantID.Value);
                        if (variant != null) variant.Stock += item.Quantity;
                    }
                }
            }

            if (order.Payment != null)
            {
                if (order.Payment.PaymentStatus == "Paid")
                    order.Payment.PaymentStatus = "Refund Initiated";
                else
                    order.Payment.PaymentStatus = "Cancelled";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(OrderHistory));
        }

        public async Task<IActionResult> ViewFeedback(int id)
        {
            int customerId = GetCurrentCustomerId();
            var feedback = await _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.OrderId == id && f.CustomerId == customerId);

            if (feedback == null)
                return NotFound();

            return View(feedback);
        }
    }
}