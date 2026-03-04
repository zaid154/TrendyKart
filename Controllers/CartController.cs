using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }
        // get user id from claims
        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
        // cart page
        public async Task<IActionResult> Index()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();
            ViewBag.CartTotal = cartItems.Sum(c => c.Product.Price * c.Quantity);
            return View(cartItems);
        }

        // upsert cart item 
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            int customerId = GetCurrentCustomerId();
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound();
            if (quantity < 1)
                quantity = 1;
            if (quantity > product.Stock)
                quantity = product.Stock;
            var existingItem = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.CustomerID == customerId &&
                    c.ProductID == productId);
            if (existingItem != null)
            {
                existingItem.Quantity = quantity;  // replace
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    CustomerID = customerId,
                    ProductID = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        // update cart item quantity
        [HttpPost]
        public async Task<IActionResult> Update(int CartID, int Quantity)
        {
            int customerId = GetCurrentCustomerId();
            var cartItem = await _context.Carts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartID == CartID && c.CustomerID == customerId);
            if (cartItem == null)
                return RedirectToAction("Index");
            if (Quantity <= 0)
            {
                _context.Carts.Remove(cartItem);
            }
            else
            {
                if (Quantity > cartItem.Product.Stock)
                    Quantity = cartItem.Product.Stock;
                cartItem.Quantity = Quantity;
            }
            await _context.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString());
        }
        // remove cart item
        [HttpPost]
        public async Task<IActionResult> Remove(int CartID)
        {
            var item = await _context.Carts.FindAsync(CartID);
            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
        // checkout page
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
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
            ViewBag.CartTotal = cartItems.Sum(c => (c.Product?.Price ?? 0) * c.Quantity);
            return View(cartItems);
        }
        // confirm checkout and create order
        [HttpPost]
        public async Task<IActionResult> ConfirmCheckout(
         string ShippingAddress,
         string City,
         string State,
         string Pincode,
         string Country)
        {
            int customerId = GetCurrentCustomerId();
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return RedirectToAction("Login", "Account");
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();
            if (!cartItems.Any())
                return RedirectToAction("Index");
            decimal totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);
            // order creation 
            var order = new Order
            {
                CustomerID = customerId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Order Placed",
                PaymentMethod = "Cash On Delivery",
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
            // create payment record
            var payment = new Payment
            {
                OrderID = order.OrderID,
                Amount = totalAmount, 
                PaymentMethod = "Cash On Delivery",
                PaymentStatus = "Pending",
                PaymentDate = null
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            //save shipping address as default for future 
            customer.DefaultStreet = ShippingAddress;
            customer.DefaultCity = City;
            customer.DefaultState = State;
            customer.DefaultPincode = Pincode;
            customer.DefaultCountry = Country;
            await _context.SaveChangesAsync();
            // create order items and update stock
            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    ProductName = item.Product.Name,
                    Price = item.Product.Price,
                    Quantity = item.Quantity,
                    ItemTotal = item.Product.Price * item.Quantity
                });
                item.Product.Stock -= item.Quantity;
            }
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderID });
        }
        // order confirmation page
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return NotFound();
            return View(order);
        }
        // remove from cart 
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            int customerId = GetCurrentCustomerId();
            var item = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.CustomerID == customerId &&
                    c.ProductID == productId);
            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

    }
}