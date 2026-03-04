using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrendyKart.Data;
using TrendyKart.Models;
namespace TrendyKart.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Get logged-in customer ID
        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
        // Load cart items
        private async Task LoadCartItems()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts.Where(c => c.CustomerID == customerId).ToListAsync();
            ViewBag.CartItems = cartItems;
        }
        // Product list with search, filter and pagination
        public async Task<IActionResult> Index(string search, string category, int page = 1)
        {
            int pageSize = 9;
            var products = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Category.Contains(search));
            }
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }
            ViewBag.Categories = await _context.Products.Select(p => p.Category).Distinct().ToListAsync();
            int totalProducts = await products.CountAsync();
            var pagedProducts = await products.OrderBy(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.CurrentPage = page;
            await LoadCartItems();
            return View(pagedProducts);
        }
        // Product details
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == id);
            if (product == null)
                return NotFound();
            int customerId = GetCurrentCustomerId();
            var cartItem = await _context.Carts.FirstOrDefaultAsync(c =>
                c.ProductID == id && c.CustomerID == customerId);
            ViewBag.CartQuantity = cartItem?.Quantity ?? 0;
            return View(product);
        }
    }
}