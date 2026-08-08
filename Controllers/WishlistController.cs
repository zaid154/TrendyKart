using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst("CustomerID") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return 0;
        }

        public async Task<IActionResult> Index()
        {
            int customerId = GetCurrentCustomerId();
            var items = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Variants)
                .Include(w => w.Variant)
                .Where(w => w.CustomerID == customerId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ToggleWishlist(int productId, int? variantId)
        {
            int customerId = GetCurrentCustomerId();
            if (customerId == 0)
                return Json(new { success = false, requireLogin = true, message = "Please login to add items to your wishlist." });

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.CustomerID == customerId && w.ProductID == productId && w.VariantID == variantId);

            bool added;
            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                added = false;
            }
            else
            {
                var wish = new Wishlist
                {
                    CustomerID = customerId,
                    ProductID = productId,
                    VariantID = variantId
                };
                _context.Wishlists.Add(wish);
                added = true;
            }

            await _context.SaveChangesAsync();
            int count = await _context.Wishlists.CountAsync(w => w.CustomerID == customerId);

            return Json(new { success = true, added, count, message = added ? "Added to wishlist!" : "Removed from wishlist." });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            int customerId = GetCurrentCustomerId();
            var item = await _context.Wishlists.FirstOrDefaultAsync(w => w.WishlistID == id && w.CustomerID == customerId);
            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            int customerId = GetCurrentCustomerId();
            if (customerId == 0) return Json(new { count = 0 });
            int count = await _context.Wishlists.CountAsync(w => w.CustomerID == customerId);
            return Json(new { count });
        }
    }
}
