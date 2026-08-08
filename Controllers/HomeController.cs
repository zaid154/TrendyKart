using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            // Load all active homepage blocks once, then split them by section.
            var blocks = await _context.HomeBlocks
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            var vm = new HomeViewModel
            {
                Hero = blocks.FirstOrDefault(b => b.Section == "Hero"),
                FeatureTiles = blocks.Where(b => b.Section == "FeatureTile").ToList(),
                CategoryChips = blocks.Where(b => b.Section == "CategoryChip").ToList(),
                PopularTiles = blocks.Where(b => b.Section == "PopularTile").ToList(),
                SaleBanner = blocks.FirstOrDefault(b => b.Section == "SaleBanner"),

                // Product grids for the homepage sections.
                NewArrivals = await _context.Products
                    .OrderByDescending(p => p.CreatedAt).Take(8).ToListAsync(),
                Bestsellers = await _context.Products
                    .Where(p => p.IsBestseller).Take(8).ToListAsync(),
                Featured = await _context.Products
                    .Where(p => p.IsFeatured).Take(8).ToListAsync(),
                Discounted = await _context.Products
                    .Where(p => p.OldPrice != null).Take(8).ToListAsync()
            };

            return View(vm);
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        // Simple contact form handler — logs the message (email wiring optional) and
        // flashes a thank-you back to the same page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string message)
        {
            _logger.LogInformation("Contact form from {Name} <{Email}>: {Message}", name, email, message);
            TempData["ContactSent"] = "Thanks for reaching out! We'll get back to you within 24 hours.";
            return RedirectToAction("Contact");
        }

        public IActionResult Blog()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}