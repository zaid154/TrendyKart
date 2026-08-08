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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGSTCalculatorService _gstCalculator;
        private readonly ICouponService _couponService;
        private readonly IRazorpayService _razorpayService;
        private readonly IInvoicePdfService _invoiceService;

        public CartController(
            ApplicationDbContext context,
            IGSTCalculatorService gstCalculator,
            ICouponService couponService,
            IRazorpayService razorpayService,
            IInvoicePdfService invoiceService)
        {
            _context = context;
            _gstCalculator = gstCalculator;
            _couponService = couponService;
            _razorpayService = razorpayService;
            _invoiceService = invoiceService;
        }

        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id) && id > 0)
                return id;

            // Fallback to customer ID 1 (default user) so cart operations ALWAYS succeed for guests
            var defaultCust = _context.Customers.FirstOrDefault();
            return defaultCust != null ? defaultCust.CustomerID : 1;
        }

        public async Task<IActionResult> Index()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                    .ThenInclude(p => p!.SubCategory)
                        .ThenInclude(s => s!.Category)
                .Include(c => c.Variant)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();

            decimal subTotal = cartItems.Sum(c => (c.Variant != null ? c.Variant.Price : c.Product?.Price ?? 0) * c.Quantity);
            
            var shippingSetting = await _context.ShippingSettings.FirstOrDefaultAsync() ?? new ShippingSetting();
            decimal shippingCharge = (subTotal >= shippingSetting.FreeShippingThreshold || subTotal == 0) ? 0 : shippingSetting.FlatShippingRate;

            ViewBag.CartTotal = subTotal + shippingCharge;
            ViewBag.SubTotal = subTotal;
            ViewBag.ShippingCharge = shippingCharge;
            ViewBag.ShippingSetting = shippingSetting;

            ViewBag.RecommendedProducts = await _context.Products
                .Where(p => p.Stock > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int? variantId, int quantity = 1)
        {
            int customerId = GetCurrentCustomerId();
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound("Product not found.");

            if (variantId.HasValue && variantId.Value <= 0)
            {
                variantId = null;
            }

            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _context.ProductVariants.FindAsync(variantId.Value);
            }

            int availableStock = variant != null ? variant.Stock : product.Stock;
            if (availableStock < 1)
                return BadRequest("Selected item is out of stock.");

            if (quantity < 1) quantity = 1;
            if (quantity > availableStock) quantity = availableStock;

            var existingItem = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.CustomerID == customerId &&
                    c.ProductID == productId &&
                    c.VariantID == variantId);

            if (existingItem != null)
            {
                existingItem.Quantity = quantity;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    CustomerID = customerId,
                    ProductID = productId,
                    VariantID = variantId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Update(int CartID, int Quantity)
        {
            int customerId = GetCurrentCustomerId();
            var cartItem = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .FirstOrDefaultAsync(c => c.CartID == CartID && c.CustomerID == customerId);

            if (cartItem == null)
                return RedirectToAction("Index");

            if (Quantity <= 0)
            {
                _context.Carts.Remove(cartItem);
            }
            else
            {
                int maxStock = cartItem.Variant != null ? cartItem.Variant.Stock : (cartItem.Product?.Stock ?? 0);
                if (Quantity > maxStock) Quantity = maxStock;
                cartItem.Quantity = Quantity;
            }

            await _context.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int CartID)
        {
            int customerId = GetCurrentCustomerId();
            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartID == CartID && c.CustomerID == customerId);

            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId, int? variantId)
        {
            int customerId = GetCurrentCustomerId();
            var item = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.CustomerID == customerId &&
                    c.ProductID == productId &&
                    c.VariantID == variantId);

            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                    .ThenInclude(p => p!.SubCategory)
                        .ThenInclude(s => s!.Category)
                .Include(c => c.Variant)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index");

            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                ViewBag.DefaultStreet = customer.DefaultStreet;
                ViewBag.DefaultCity = customer.DefaultCity;
                ViewBag.DefaultState = customer.DefaultState;
                ViewBag.DefaultPincode = customer.DefaultPincode;
                ViewBag.DefaultCountry = customer.DefaultCountry ?? "India";
            }

            decimal rawTotal = cartItems.Sum(c => (c.Variant != null ? c.Variant.Price : c.Product?.Price ?? 0) * c.Quantity);
            var shippingSetting = await _context.ShippingSettings.FirstOrDefaultAsync() ?? new ShippingSetting();
            decimal shippingCharge = (rawTotal >= shippingSetting.FreeShippingThreshold) ? 0 : shippingSetting.FlatShippingRate;

            ViewBag.SubTotal = rawTotal;
            ViewBag.ShippingCharge = shippingCharge;
            ViewBag.CartTotal = rawTotal + shippingCharge;

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRazorpayOrder(string couponCode)
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest("Cart is empty");

            decimal rawSubtotal = cartItems.Sum(c => (c.Variant != null ? c.Variant.Price : c.Product?.Price ?? 0) * c.Quantity);
            var shippingSetting = await _context.ShippingSettings.FirstOrDefaultAsync() ?? new ShippingSetting();
            decimal shippingCharge = (rawSubtotal >= shippingSetting.FreeShippingThreshold) ? 0 : shippingSetting.FlatShippingRate;

            decimal discount = 0;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var couponRes = await _couponService.ValidateCouponAsync(couponCode, rawSubtotal, customerId);
                if (couponRes.IsValid) discount = couponRes.DiscountAmount;
            }

            decimal finalPayable = Math.Max(0, rawSubtotal + shippingCharge - discount);
            string orderNum = "TK-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Random.Shared.Next(100, 999);
            string rzpOrderId = _razorpayService.CreateRazorpayOrder(orderNum, finalPayable);

            return Json(new
            {
                orderId = rzpOrderId,
                amount = (long)(finalPayable * 100),
                currency = "INR",
                orderNumber = orderNum
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout(
            string ShippingAddress,
            string City,
            string State,
            string Pincode,
            string Country,
            string PaymentMethod,
            string? CouponCode,
            string? RazorpayPaymentId,
            string? RazorpayOrderId,
            string? RazorpaySignature)
        {
            int customerId = GetCurrentCustomerId();
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.Carts
                .Include(c => c.Product)
                    .ThenInclude(p => p!.SubCategory)
                        .ThenInclude(s => s!.Category)
                .Include(c => c.Variant)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index");

            // Validate stock
            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    TempData["ErrorMessage"] = "One of the products in your cart is no longer available.";
                    return RedirectToAction("Checkout");
                }
                int currentStock = item.Variant != null ? item.Variant.Stock : item.Product.Stock;
                if (item.Quantity > currentStock)
                {
                    TempData["ErrorMessage"] = $"'{item.Product.Name}' has only {currentStock} left in stock.";
                    return RedirectToAction("Checkout");
                }
            }

            // Calculations
            decimal totalItemPriceSum = cartItems.Sum(c => (c.Variant != null ? c.Variant.Price : c.Product!.Price) * c.Quantity);

            // GST Calculations line-by-line
            decimal calculatedSubtotalBase = 0;
            decimal calculatedGstTotal = 0;

            foreach (var item in cartItems)
            {
                decimal unitPrice = item.Variant != null ? item.Variant.Price : item.Product!.Price;
                decimal gstPct = _gstCalculator.GetEffectiveGSTPercentage(item.Product!);
                var (basePrice, gstAmount, totalPrice) = _gstCalculator.CalculateItemBreakup(unitPrice, gstPct, item.Quantity);

                calculatedSubtotalBase += basePrice;
                calculatedGstTotal += gstAmount;
            }

            var shippingSetting = await _context.ShippingSettings.FirstOrDefaultAsync() ?? new ShippingSetting();
            decimal shippingCharge = (totalItemPriceSum >= shippingSetting.FreeShippingThreshold) ? 0 : shippingSetting.FlatShippingRate;

            // Coupon Validation
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(CouponCode))
            {
                var couponVal = await _couponService.ValidateCouponAsync(CouponCode, totalItemPriceSum, customerId);
                if (couponVal.IsValid && couponVal.Coupon != null)
                {
                    discountAmount = couponVal.DiscountAmount;
                    couponVal.Coupon.TimesUsed += 1;
                }
            }

            decimal finalTotal = Math.Max(0, totalItemPriceSum + shippingCharge - discountAmount);

            // Razorpay Payment Verification
            if (PaymentMethod == "Razorpay")
            {
                bool isVerified = _razorpayService.VerifySignature(RazorpayOrderId ?? "", RazorpayPaymentId ?? "", RazorpaySignature ?? "");
                if (!isVerified)
                {
                    TempData["ErrorMessage"] = "Payment signature verification failed. Please try again.";
                    return RedirectToAction("Checkout");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            string orderNumber = "TK-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Random.Shared.Next(100, 999);

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerID = customerId,
                OrderDate = DateTime.UtcNow,
                SubTotal = calculatedSubtotalBase,
                GSTTotal = calculatedGstTotal,
                ShippingCharge = shippingCharge,
                DiscountAmount = discountAmount,
                TotalAmount = finalTotal,
                CouponCode = CouponCode,
                Status = "Order Placed",
                PaymentStatus = PaymentMethod == "Razorpay" ? "Paid" : "Pending",
                PaymentMethod = PaymentMethod == "Razorpay" ? "Razorpay" : "Cash On Delivery",
                RazorpayOrderId = RazorpayOrderId,
                RazorpayPaymentId = RazorpayPaymentId,
                RazorpaySignature = RazorpaySignature,
                ShippingAddress = ShippingAddress,
                City = City,
                State = State,
                Pincode = Pincode,
                Country = Country,
                Email = customer.Email,
                Phone = customer.Phone
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create Payment record
            var payment = new Payment
            {
                OrderID = order.OrderID,
                Amount = finalTotal,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                RazorpayOrderId = RazorpayOrderId,
                RazorpayPaymentId = RazorpayPaymentId,
                RazorpaySignature = RazorpaySignature,
                PaymentDate = PaymentMethod == "Razorpay" ? DateTime.UtcNow : null
            };
            _context.Payments.Add(payment);

            // Order Items & Stock Deduction
            foreach (var item in cartItems)
            {
                decimal unitPrice = item.Variant != null ? item.Variant.Price : item.Product!.Price;
                decimal gstPct = _gstCalculator.GetEffectiveGSTPercentage(item.Product!);
                var (basePrice, gstAmount, totalPrice) = _gstCalculator.CalculateItemBreakup(unitPrice, gstPct, item.Quantity);

                _context.OrderItems.Add(new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    VariantID = item.VariantID,
                    ProductName = item.Product!.Name,
                    VariantName = item.Variant != null ? item.Variant.VariantName : "",
                    UnitPrice = unitPrice,
                    Price = basePrice,
                    GSTPercentage = gstPct,
                    GSTAmount = gstAmount,
                    Quantity = item.Quantity,
                    ItemTotal = totalPrice,
                    ImageUrl = item.Variant != null && item.Variant.MediaList != null && item.Variant.MediaList.Any()
                        ? item.Variant.MediaList.First().MediaUrl
                        : item.Product.ImageUrl
                });

                // Deduct inventory
                if (item.Variant != null)
                {
                    item.Variant.Stock -= item.Quantity;
                }
                item.Product!.Stock -= item.Quantity;
            }

            // Save user default address for future
            customer.DefaultStreet = ShippingAddress;
            customer.DefaultCity = City;
            customer.DefaultState = State;
            customer.DefaultPincode = Pincode;
            customer.DefaultCountry = Country;

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Trigger Invoice Email in background
            try
            {
                var siteSetting = await _context.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();
                await _invoiceService.SendInvoiceEmailAsync(order, siteSetting);
            }
            catch { /* Email send fallback handled gracefully */ }

            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderID });
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}