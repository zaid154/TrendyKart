using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyKart.Data;
using TrendyKart.Models;
namespace TrendyKart.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }
        // custom method to get current logged in customer's ID
        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
        //orders page for customers
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
        //order details page for customers
        public async Task<IActionResult> OrderDetails(int id)
        {
            int customerId = GetCurrentCustomerId();

            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null)
                return NotFound();

            return View(order);
        }
        //give feedback page for customers
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
        //submit feedback for customers
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int orderId, int rating, string comment)
        {
            int customerId = GetCurrentCustomerId();

            var feedback = await _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.OrderId == orderId && f.CustomerId == customerId);

            if (feedback == null)
            {
                // 1 time feedback create
                feedback = new OrderFeedback
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    Rating = rating,
                    IsReadByAdmin = false,
                    CreatedAt = DateTime.Now,
                    Messages = new List<FeedbackMessage>()
                };

                _context.OrderFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();
            }
            // Add message (for both new and existing feedback)
            var newMessage = new FeedbackMessage
            {
                FeedbackId = feedback.Id,
                SenderRole = "Customer", 
                Message = comment,
                SentAt = DateTime.Now
            };
            _context.FeedbackMessages.Add(newMessage);
            feedback.IsReadByAdmin = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Message sent successfully.";
            return RedirectToAction("ViewFeedback", new { id = orderId });
        }
        // cancel order for customers
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
            if (order.Status != "Order Placed")
            {
                TempData["ErrorMessage"] = "Order cannot be cancelled after shipping.";
                return RedirectToAction(nameof(OrderHistory));
            }
            order.Status = "Cancelled";
            // Restore stock
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductID);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                    }
                }
            }
            // Update payment status
            if (order.Payment != null)
            {
                if (order.Payment.PaymentStatus == "Paid")
                {
                    order.Payment.PaymentStatus = "Refund Initiated";
                }
                else
                {
                    order.Payment.PaymentStatus = "Cancelled";
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(OrderHistory));
        }
        // view feedback and messages for customers
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