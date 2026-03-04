using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyKart.Data;
using TrendyKart.Models;
namespace TrendyKart.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.TotalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var ratingData = await _context.OrderFeedbacks
                .GroupBy(f => f.Rating)
                .Select(g => new
                {
                    Rating = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Rating)
                .ToListAsync();
            ViewBag.RatingData = ratingData;
            return View();
        }
        // Payments view 
        public IActionResult Payments()
        {
            var payments = _context.Payments
                .Include(p => p.Order)
                .ToList();

            return View("~/Views/Payment/Index.cshtml", payments);
        }
        // Products view
        public IActionResult Products()
        {
            var products = _context.Products.ToList();
            return View(products);
        }
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.ImageFile == null)
            {
                ModelState.AddModelError("", "Product image is required.");
                return View(model);
            }
            string fileName = Guid.NewGuid() +
                              Path.GetExtension(model.ImageFile.FileName);
            string uploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/images",
                fileName
            );
            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }
            model.ImageUrl = "/images/" + fileName;
            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Products");
        }
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();
            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product model)
        {
            var product = await _context.Products.FindAsync(model.ProductID);
            if (product == null)
                return NotFound();
            product.Name = model.Name;
            product.Description = model.Description;
            product.Category = model.Category;
            product.Price = model.Price;
            product.Stock = model.Stock;
            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid() +
                                  Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images",
                    fileName
                );
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = "/images/" + fileName;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Products");
        }
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();
            _context.Products.Remove(product);
            _context.SaveChanges();
            return RedirectToAction("Products");
        }
        // customers view
        public IActionResult Customers()
        {
            var users = _context.Customers.ToList();
            return View(users);
        }
        // block users
        public IActionResult BlockUser(int id)
        {
            var user = _context.Customers.Find(id);
            if (user == null)
                return NotFound();
            var currentUserId =
                int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.CustomerID == currentUserId)
                return RedirectToAction(nameof(Customers));
            user.IsBlocked = true;
            _context.SaveChanges();
            return RedirectToAction(nameof(Customers));
        }
        // unblock users
        public IActionResult UnblockUser(int id)
        {
            var user = _context.Customers.Find(id);
            if (user == null)
                return NotFound();
            user.IsBlocked = false;
            _context.SaveChanges();
            return RedirectToAction(nameof(Customers));
        }
        // customer details view
        public IActionResult CustomerDetails(int id)
        {
            var user = _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefault(c => c.CustomerID == id);
            if (user == null)
                return NotFound();
            return View(user);
        }
        // feedback management views
        public IActionResult FeedbackList()
        {
            var feedbacks = _context.OrderFeedbacks
                .Include(f => f.Messages)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
            return View(feedbacks);
        }
        public IActionResult FeedbackDetails(int id)
        {
            var feedback = _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefault(f => f.Id == id);
            if (feedback == null)
                return NotFound();
            feedback.IsReadByAdmin = true;
            _context.SaveChanges();
            return View(feedback);
        }
        // send message to customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAdminMessage(int feedbackId, string message)
        {
            var feedback = await _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);
            if (feedback == null)
                return NotFound();
            feedback.Messages.Add(new FeedbackMessage
            {
                SenderRole = "Admin",
                Message = message,
                SentAt = DateTime.Now
            });
            feedback.IsReadByAdmin = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("FeedbackDetails", new { id = feedbackId });
        }
        // update payment status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int paymentId, string status)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);
            if (payment == null)
                return NotFound();
            // Mark Paid
            if (status == "Paid")
            {
                payment.PaymentStatus = "Paid";
                payment.PaymentDate = DateTime.Now;
            }
            // Mark Failed
            else if (status == "Failed")
            {
                payment.PaymentStatus = "Failed";
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Payment status updated successfully.";
            return RedirectToAction("Payments");
        }

        // update refund status

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteRefund(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);
            if (payment == null)
                return NotFound();
            if (payment.PaymentStatus != "Refund Initiated")
            {
                TempData["ErrorMessage"] = "Refund not eligible.";
                return RedirectToAction("Payments");
            }
            payment.PaymentStatus = "Refund Completed";
            payment.PaymentDate = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Refund marked as completed.";
            return RedirectToAction("Payments");
        }
        // order management view
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }
        // update order status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return NotFound();
            if (order.Status == "Delivered" ||
                order.Status == "Cancelled" ||
                order.Status == "Refunded")
            {
                TempData["ErrorMessage"] = "Completed orders cannot be modified.";
                return RedirectToAction("Orders");
            }
            // Cancel logic
            if (status == "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);
                    if (product != null)
                        product.Stock += item.Quantity;
                }
                if (order.Payment != null)
                {
                    if (order.Payment.PaymentStatus == "Paid")
                        order.Payment.PaymentStatus = "Refund Initiated";
                    else
                        order.Payment.PaymentStatus = "Cancelled";
                }
            }

            // COD auto paid
            if (status == "Delivered" &&
                order.PaymentMethod == "Cash On Delivery" &&
                order.Payment != null)
            {
                order.Payment.PaymentStatus = "Paid";
                order.Payment.PaymentDate = DateTime.Now;
            }
            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Order status updated.";
            return RedirectToAction("Orders");
        }   

    }
}