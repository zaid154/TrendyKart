using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TrendyKart.Data;
using TrendyKart.Models;
using TrendyKart.Services;
using TrendyKart.ViewModels;

namespace TrendyKart.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public AdminController(ApplicationDbContext context, IEmailService emailService, IAuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.TotalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCoupons = await _context.Coupons.CountAsync();

            var ratingData = await _context.OrderFeedbacks
                .GroupBy(f => f.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .OrderBy(x => x.Rating)
                .ToListAsync();
            ViewBag.RatingData = ratingData;

            return View();
        }

        public IActionResult Payments()
        {
            var payments = _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
            return View(payments);
        }

        // ==================== PRODUCTS & VARIANTS ====================

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.CategoryRef)
                .Include(p => p.SubCategory)
                .Include(p => p.Variants)
                .Include(p => p.MediaFiles)
                .ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            ViewBag.Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product model, List<IFormFile>? mediaFiles)
        {
            ViewBag.Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();

            if (model.ImageFile != null)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".avif" };
                var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(ext))
                {
                    ModelState.AddModelError("", "Only image files (.jpg, .jpeg, .png, .webp, .avif) are allowed.");
                    return View(model);
                }

                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Product main image size must be 5MB or less.");
                    return View(model);
                }
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                string fileName = await SaveUploadedFilePreservingNameAsync(model.ImageFile, uploadsDir);
                model.ImageUrl = "/uploads/" + fileName;
            }
            else
            {
                model.ImageUrl = "/images/no-image.png";
            }

            model.CreatedAt = DateTime.UtcNow;

            // Process nested variants submitted directly from Add Product form
            if (model.Variants == null || !model.Variants.Any(v => !string.IsNullOrWhiteSpace(v.VariantName)))
            {
                model.Variants = new List<ProductVariant>
                {
                    new ProductVariant
                    {
                        VariantName = "Standard / Base Edition",
                        SKU = string.IsNullOrWhiteSpace(model.SKU) ? $"SKU-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}" : model.SKU,
                        Price = model.Price,
                        OldPrice = model.OldPrice,
                        Stock = model.Stock,
                        IsDefault = true,
                        IsActive = true
                    }
                };
            }
            else
            {
                model.Variants = model.Variants
                    .Where(v => !string.IsNullOrWhiteSpace(v.VariantName))
                    .ToList();

                if (!model.Variants.Any(v => v.IsDefault))
                {
                    model.Variants.First().IsDefault = true;
                }
            }

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            // Multi-media upload handler (Images <= 5MB, Videos <= 10MB)
            if (mediaFiles != null && mediaFiles.Any())
            {
                int sort = 1;
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                foreach (var file in mediaFiles)
                {
                    string ext = Path.GetExtension(file.FileName).ToLower();
                    bool isVideo = ext == ".mp4" || ext == ".webm";
                    long maxAllowed = isVideo ? 10 * 1024 * 1024 : 5 * 1024 * 1024;

                    if (file.Length > maxAllowed) continue;

                    string mediaFileName = await SaveUploadedFilePreservingNameAsync(file, uploadsDir);

                    _context.ProductMediaFiles.Add(new ProductMedia
                    {
                        ProductID = model.ProductID,
                        MediaType = isVideo ? "Video" : "Image",
                        MediaUrl = "/uploads/" + mediaFileName,
                        FileSize = file.Length,
                        SortOrder = sort++
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Product '{model.Name}' created successfully with {model.Variants.Count} variant(s).";
            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.MediaFiles)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                // Fallback to first existing product if ID not found
                product = await _context.Products
                    .Include(p => p.Variants)
                    .Include(p => p.MediaFiles)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    TempData["ErrorMessage"] = "No products found in the database. Please add a product first.";
                    return RedirectToAction("AddProduct");
                }
            }

            if (product != null && product.Name.Contains("iPhone 17 Pro Max") && product.Variants != null && product.Variants.Any())
            {
                var vList = product.Variants.ToList();

                // Variant 1: Space Black 256GB
                if (vList.Count >= 1)
                {
                    vList[0].VariantName = "Space Black - 256GB";
                    vList[0].SKU = "IP17PM-256-BLK";
                    vList[0].Price = 144900;
                    vList[0].OldPrice = 154900;
                    vList[0].Stock = 25;
                    vList[0].ColorName = "Space Black";
                    vList[0].ColorHex = "#1C1B1B";
                    vList[0].ImageUrl = "/uploads/demo/iphone-17-pro.png";
                    vList[0].ShortDescription = "256GB Storage with Grade 5 Titanium in Space Black finish.";
                    vList[0].LongDescription = "iPhone 17 Pro Max 256GB in Space Black features the A19 Pro 3nm chip with 6-core GPU, ProMotion 120Hz Super Retina XDR OLED display, 48MP Fusion camera system, and all-day battery life.";
                    if (string.IsNullOrWhiteSpace(vList[0].SpecificationsJson) || vList[0].SpecificationsJson == "{}")
                    {
                        vList[0].SpecificationsJson = "Storage: 256GB | RAM: 12GB | Display: 6.9 inch Super Retina XDR OLED 120Hz ProMotion | Processor: Apple A19 Pro (3nm) | Camera: 48MP Main + 48MP Ultra Wide + 48MP Telephoto | Battery: 4850 mAh with 35W Fast Charge | Build: Grade 5 Titanium Frame | OS: iOS 19";
                    }
                    vList[0].IsDefault = true;
                }

                // Variant 2: Natural Titanium 512GB
                if (vList.Count >= 2)
                {
                    vList[1].VariantName = "Natural Titanium - 512GB";
                    vList[1].SKU = "IP17PM-512-NAT";
                    vList[1].Price = 164900;
                    vList[1].OldPrice = 174900;
                    vList[1].Stock = 18;
                    vList[1].ColorName = "Natural Titanium";
                    vList[1].ColorHex = "#B8B5AD";
                    vList[1].ImageUrl = "/uploads/demo/iphone-17-pro.png";
                    vList[1].ShortDescription = "512GB High-speed storage with brushed Natural Titanium finish.";
                    vList[1].LongDescription = "iPhone 17 Pro Max 512GB in Natural Titanium delivers massive high-speed storage for 4K ProRes video recording at 60 fps, dual eSIM support, second-gen Sensor-shift OIS, and USB-C 10Gbps transfers.";
                    if (string.IsNullOrWhiteSpace(vList[1].SpecificationsJson) || vList[1].SpecificationsJson == "{}")
                    {
                        vList[1].SpecificationsJson = "Storage: 512GB | RAM: 16GB | Display: 6.9 inch Super Retina XDR OLED 120Hz ProMotion | Processor: Apple A19 Pro (3nm) | Camera: 48MP Main + 48MP Ultra Wide + 48MP Telephoto | Battery: 4850 mAh with 35W Fast Charge | Build: Grade 5 Titanium Frame | OS: iOS 19";
                    }
                }

                // Variant 3: Deep White 1TB
                if (vList.Count >= 3)
                {
                    vList[2].VariantName = "Deep White - 1TB";
                    vList[2].SKU = "IP17PM-1TB-WHT";
                    vList[2].Price = 184900;
                    vList[2].OldPrice = 194900;
                    vList[2].Stock = 12;
                    vList[2].ColorName = "Deep White";
                    vList[2].ColorHex = "#F4F4F0";
                    vList[2].ImageUrl = "/uploads/demo/Apple iPhone 14 Pro 512GB Gold.webp";
                    vList[2].ShortDescription = "1TB Ultra storage for professional content creators in Deep White.";
                    vList[2].LongDescription = "The flagship 1TB iPhone 17 Pro Max in Deep White provides maximum storage capacity for LOG video recording, spatial video capture for Apple Vision Pro, and hardware-accelerated Ray Tracing.";
                    if (string.IsNullOrWhiteSpace(vList[2].SpecificationsJson) || vList[2].SpecificationsJson == "{}")
                    {
                        vList[2].SpecificationsJson = "Storage: 1TB | RAM: 16GB | Display: 6.9 inch Super Retina XDR OLED 120Hz ProMotion | Processor: Apple A19 Pro (3nm) | Camera: 48MP Main + 48MP Ultra Wide + 48MP Telephoto | Battery: 4850 mAh with 35W Fast Charge | Build: Grade 5 Titanium Frame | OS: iOS 19";
                    }
                }

                // Variant 4: Dark Blue 256GB
                if (vList.Count >= 4)
                {
                    vList[3].VariantName = "Dark Blue - 256GB";
                    vList[3].SKU = "IP17PM-256-BLU";
                    vList[3].Price = 144900;
                    vList[3].OldPrice = 154900;
                    vList[3].Stock = 20;
                    vList[3].ColorName = "Dark Blue";
                    vList[3].ColorHex = "#1E293B";
                    vList[3].ImageUrl = "/uploads/demo/Apple iPhone 14 Pro 128GB Deep Purple.webp";
                    vList[3].ShortDescription = "256GB Edition in deep Sapphire Dark Blue titanium.";
                    vList[3].LongDescription = "iPhone 17 Pro Max 256GB in Dark Blue combines elegant midnight blue hues with next-generation Wi-Fi 7 connectivity, Emergency SOS via satellite, and Roadside Assistance via satellite.";
                    if (string.IsNullOrWhiteSpace(vList[3].SpecificationsJson) || vList[3].SpecificationsJson == "{}")
                    {
                        vList[3].SpecificationsJson = "Storage: 256GB | RAM: 12GB | Display: 6.9 inch Super Retina XDR OLED 120Hz ProMotion | Processor: Apple A19 Pro (3nm) | Camera: 48MP Main + 48MP Ultra Wide + 48MP Telephoto | Battery: 4850 mAh with 35W Fast Charge | Build: Grade 5 Titanium Frame | OS: iOS 19";
                    }
                }

                // Variant 5: Titanium Gold 1TB
                if (vList.Count >= 5)
                {
                    vList[4].VariantName = "Titanium Gold - 1TB";
                    vList[4].SKU = "IP17PM-1TB-GLD";
                    vList[4].Price = 184900;
                    vList[4].OldPrice = 194900;
                    vList[4].Stock = 8;
                    vList[4].ColorName = "Titanium Gold";
                    vList[4].ColorHex = "#E5C158";
                    vList[4].ImageUrl = "/uploads/demo/Apple iPhone 14 Pro 512GB Gold.webp";
                    vList[4].ShortDescription = "1TB Collector's Titanium Gold edition.";
                    vList[4].LongDescription = "Exclusive 1TB Titanium Gold edition of iPhone 17 Pro Max offering ultra-capacity storage, gold PVD coating over Grade 5 titanium, and 5x Optical Zoom.";
                    if (string.IsNullOrWhiteSpace(vList[4].SpecificationsJson) || vList[4].SpecificationsJson == "{}")
                    {
                        vList[4].SpecificationsJson = "Storage: 1TB | RAM: 16GB | Display: 6.9 inch Super Retina XDR OLED 120Hz ProMotion | Processor: Apple A19 Pro (3nm) | Camera: 48MP Main + 48MP Ultra Wide + 48MP Telephoto | Battery: 4850 mAh with 35W Fast Charge | Build: Grade 5 Titanium Frame | OS: iOS 19";
                    }
                }

                if (product.MediaFiles == null || !product.MediaFiles.Any())
                {
                    _context.ProductMediaFiles.AddRange(
                        new ProductMedia { ProductID = product.ProductID, MediaType = "Image", MediaUrl = "/uploads/demo/iphone-17-pro.png" },
                        new ProductMedia { ProductID = product.ProductID, MediaType = "Image", MediaUrl = "/uploads/demo/Apple iPhone 14 Pro 128GB Deep Purple.webp" },
                        new ProductMedia { ProductID = product.ProductID, MediaType = "Image", MediaUrl = "/uploads/demo/Apple iPhone 14 Pro 512GB Gold.webp" },
                        new ProductMedia { ProductID = product.ProductID, MediaType = "Video", MediaUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4" }
                    );
                }

                await _context.SaveChangesAsync();
            }

            ViewBag.Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();
            return View(product);
        }

        public async Task<IActionResult> DeleteProductMedia(int id)
        {
            var media = await _context.ProductMediaFiles.FindAsync(id);
            if (media != null)
            {
                int productId = media.ProductID;
                _context.ProductMediaFiles.Remove(media);
                await _context.SaveChangesAsync();
                return RedirectToAction("EditProduct", new { id = productId });
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product model, List<IFormFile>? mediaFiles)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID);

            if (product == null) return NotFound();

            product.Name = model.Name;
            product.Description = model.Description;
            product.CategoryID = model.CategoryID;
            product.SubCategoryID = model.SubCategoryID;
            product.Brand = model.Brand;
            product.SKU = model.SKU;
            product.Price = model.Price;
            product.OldPrice = model.OldPrice;
            product.Stock = model.Stock;
            product.IsFeatured = model.IsFeatured;
            product.IsBestseller = model.IsBestseller;
            product.FreeShipping = model.FreeShipping;
            product.GSTOverridePercentage = model.GSTOverridePercentage;
            product.SpecificationsJson = model.SpecificationsJson;

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            if (model.ImageFile != null)
            {
                if (model.ImageFile.Length <= 5 * 1024 * 1024)
                {
                    string fileName = await SaveUploadedFilePreservingNameAsync(model.ImageFile, uploadsDir);
                    product.ImageUrl = "/uploads/" + fileName;
                }
            }

            if (mediaFiles != null && mediaFiles.Any())
            {
                foreach (var file in mediaFiles)
                {
                    string ext = Path.GetExtension(file.FileName).ToLower();
                    bool isVideo = ext == ".mp4" || ext == ".webm";
                    long maxAllowed = isVideo ? 10 * 1024 * 1024 : 5 * 1024 * 1024;
                    if (file.Length > maxAllowed) continue;

                    string mediaFileName = await SaveUploadedFilePreservingNameAsync(file, uploadsDir);

                    _context.ProductMediaFiles.Add(new ProductMedia
                    {
                        ProductID = product.ProductID,
                        MediaType = isVideo ? "Video" : "Image",
                        MediaUrl = "/uploads/" + mediaFileName,
                        FileSize = file.Length
                    });
                }
            }

            // Synchronize nested variants submitted from Edit Product form
            if (model.Variants != null && model.Variants.Any())
            {
                var submittedVariants = model.Variants
                    .Where(v => !string.IsNullOrWhiteSpace(v.VariantName))
                    .ToList();

                var submittedIds = submittedVariants
                    .Where(v => v.VariantID > 0)
                    .Select(v => v.VariantID)
                    .ToList();

                // Remove deleted variants
                var toRemove = product.Variants.Where(v => !submittedIds.Contains(v.VariantID)).ToList();
                foreach (var rem in toRemove)
                {
                    _context.ProductVariants.Remove(rem);
                }

                // Update existing or add new variants
                foreach (var v in submittedVariants)
                {
                    // Handle local device image file upload for variant
                    if (v.ImageFile != null && v.ImageFile.Length <= 5 * 1024 * 1024)
                    {
                        string vFileName = await SaveUploadedFilePreservingNameAsync(v.ImageFile, uploadsDir);
                        v.ImageUrl = "/uploads/" + vFileName;
                    }

                    if (v.VariantID > 0)
                    {
                        var existing = product.Variants.FirstOrDefault(x => x.VariantID == v.VariantID);
                        if (existing != null)
                        {
                            existing.VariantName = v.VariantName;
                            existing.SKU = v.SKU;
                            existing.Price = v.Price;
                            existing.OldPrice = v.OldPrice;
                            existing.Stock = v.Stock;
                            existing.Storage = v.Storage;
                            existing.RAM = v.RAM;
                            existing.ColorName = v.ColorName;
                            existing.ColorHex = v.ColorHex;
                            existing.Processor = v.Processor;
                            existing.ShortDescription = v.ShortDescription;
                            existing.LongDescription = v.LongDescription;
                            existing.SpecificationsJson = v.SpecificationsJson;
                            if (!string.IsNullOrWhiteSpace(v.ImageUrl))
                            {
                                existing.ImageUrl = v.ImageUrl;
                            }
                            existing.IsDefault = v.IsDefault;
                            existing.IsActive = v.IsActive;
                        }
                    }
                    else
                    {
                        v.ProductID = product.ProductID;
                        _context.ProductVariants.Add(v);
                    }
                }

                // Auto-sync base product Price from default or first variant
                var primaryVariant = product.Variants.FirstOrDefault(v => v.IsDefault) ?? product.Variants.FirstOrDefault();
                if (primaryVariant != null && primaryVariant.Price > 0)
                {
                    product.Price = primaryVariant.Price;
                    product.OldPrice = primaryVariant.OldPrice;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product & variants updated successfully.";
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product deleted successfully.";
            return RedirectToAction("Products");
        }

        // Product Variant CRUD
        [HttpPost]
        public async Task<IActionResult> AddVariant(int productId, ProductVariant variant)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            variant.ProductID = productId;
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product variant added.";
            return RedirectToAction("EditProduct", new { id = productId });
        }

        public async Task<IActionResult> DeleteVariant(int id, int productId)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant != null)
            {
                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("EditProduct", new { id = productId });
        }

        // ==================== CATEGORIES & GST ====================

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string name, decimal gstPercentage)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var cat = new Category
                {
                    Name = name,
                    Slug = name.ToLower().Replace(" ", "-"),
                    GSTPercentage = gstPercentage
                };
                _context.Categories.Add(cat);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category created.";
            }
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> AddSubCategory(int categoryId, string name, decimal? gstPercentage)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var sub = new SubCategory
                {
                    CategoryID = categoryId,
                    Name = name,
                    Slug = name.ToLower().Replace(" ", "-"),
                    GSTPercentage = gstPercentage
                };
                _context.SubCategories.Add(sub);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sub-Category created.";
            }
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategoryGST(int categoryId, decimal gstPercentage)
        {
            var cat = await _context.Categories.FindAsync(categoryId);
            if (cat != null)
            {
                cat.GSTPercentage = gstPercentage;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category GST updated.";
            }
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSubCategoryGST(int subCategoryId, decimal? gstPercentage)
        {
            var sub = await _context.SubCategories.FindAsync(subCategoryId);
            if (sub != null)
            {
                sub.GSTPercentage = gstPercentage;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sub-Category GST updated.";
            }
            return RedirectToAction("Categories");
        }

        // ==================== CATEGORY FILTER ATTRIBUTES ====================

        public async Task<IActionResult> FilterAttributes()
        {
            ViewBag.Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();
            var attrs = await _context.CategoryFilterAttributes
                .Include(a => a.Category)
                .Include(a => a.SubCategory)
                .ToListAsync();
            return View(attrs);
        }

        [HttpPost]
        public async Task<IActionResult> AddFilterAttribute(int? categoryId, int? subCategoryId, string attributeName, string optionsJson)
        {
            if (!string.IsNullOrEmpty(attributeName))
            {
                var attr = new CategoryFilterAttribute
                {
                    CategoryID = categoryId,
                    SubCategoryID = subCategoryId,
                    AttributeName = attributeName,
                    AttributeType = "Select",
                    OptionsJson = string.IsNullOrEmpty(optionsJson) ? "[]" : optionsJson
                };
                _context.CategoryFilterAttributes.Add(attr);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Filter attribute added.";
            }
            return RedirectToAction("FilterAttributes");
        }

        public async Task<IActionResult> DeleteFilterAttribute(int id)
        {
            var attr = await _context.CategoryFilterAttributes.FindAsync(id);
            if (attr != null)
            {
                _context.CategoryFilterAttributes.Remove(attr);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("FilterAttributes");
        }

        // ==================== COUPONS MANAGEMENT ====================

        public async Task<IActionResult> Coupons()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return View(coupons);
        }

        [HttpGet]
        public IActionResult AddCoupon() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCoupon(Coupon coupon)
        {
            if (!ModelState.IsValid) return View(coupon);

            coupon.Code = coupon.Code.ToUpper().Trim();
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Coupon created successfully.";
            return RedirectToAction("Coupons");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Coupons");
        }

        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Coupons");
        }

        // ==================== SHIPPING & SITE SETTINGS ====================

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var shipping = await _context.ShippingSettings.FirstOrDefaultAsync() ?? new ShippingSetting();
            var site = await _context.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();

            ViewBag.ShippingSetting = shipping;
            ViewBag.SiteSetting = site;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingSettings(ShippingSetting model)
        {
            var shipping = await _context.ShippingSettings.FirstOrDefaultAsync();
            if (shipping == null)
            {
                _context.ShippingSettings.Add(model);
            }
            else
            {
                shipping.FreeShippingThreshold = model.FreeShippingThreshold;
                shipping.FlatShippingRate = model.FlatShippingRate;
                shipping.ShippingInfoText = model.ShippingInfoText;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Shipping settings updated.";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSiteSettings(SiteSetting model, IFormFile? signatureFile)
        {
            var site = await _context.SiteSettings.FirstOrDefaultAsync();
            if (site == null)
            {
                site = new SiteSetting();
                _context.SiteSettings.Add(site);
            }

            site.StoreName = model.StoreName;
            site.ContactEmail = model.ContactEmail;
            site.ContactPhone = model.ContactPhone;
            site.Address = model.Address;

            if (signatureFile != null)
            {
                if (signatureFile.Length <= 5 * 1024 * 1024)
                {
                    string fileName = "signature_" + Guid.NewGuid() + Path.GetExtension(signatureFile.FileName);
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads"));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await signatureFile.CopyToAsync(stream);
                    site.AuthorizedSignatureUrl = "/uploads/" + fileName;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Site & Invoice Signature settings saved.";
            return RedirectToAction("Settings");
        }

        // ==================== HOMEPAGE CONTENT ====================

        public IActionResult HomeContent()
        {
            var blocks = _context.HomeBlocks
                .OrderBy(b => b.Section)
                .ThenBy(b => b.SortOrder)
                .ToList();
            return View(blocks);
        }

        [HttpGet]
        public IActionResult EditHomeBlock(int id)
        {
            var block = _context.HomeBlocks.Find(id);
            if (block == null) return NotFound();
            return View(block);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHomeBlock(HomeBlock model)
        {
            var block = await _context.HomeBlocks.FindAsync(model.Id);
            if (block == null) return NotFound();

            block.Eyebrow = model.Eyebrow;
            block.Title = model.Title;
            block.Subtitle = model.Subtitle;
            block.ButtonText = model.ButtonText;
            block.LinkUrl = model.LinkUrl;
            block.Size = model.Size;
            block.Theme = model.Theme;
            block.SortOrder = model.SortOrder;
            block.IsActive = model.IsActive;

            if (model.ImageFile != null)
            {
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                string fileName = await SaveUploadedFilePreservingNameAsync(model.ImageFile, uploadsDir);
                block.ImageUrl = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Homepage block updated.";
            return RedirectToAction("HomeContent");
        }

        // ==================== CUSTOMERS & ORDERS ====================

        public IActionResult Customers()
        {
            var users = _context.Customers.ToList();
            return View(users);
        }

        public IActionResult BlockUser(int id)
        {
            var user = _context.Customers.Find(id);
            if (user == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.CustomerID == currentUserId)
                return RedirectToAction(nameof(Customers));

            user.IsBlocked = true;
            _context.SaveChanges();
            return RedirectToAction(nameof(Customers));
        }

        public IActionResult UnblockUser(int id)
        {
            var user = _context.Customers.Find(id);
            if (user == null) return NotFound();
            user.IsBlocked = false;
            _context.SaveChanges();
            return RedirectToAction(nameof(Customers));
        }

        public IActionResult CustomerDetails(int id)
        {
            var user = _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefault(c => c.CustomerID == id);

            if (user == null) return NotFound();
            return View(user);
        }

        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null) return NotFound();

            if (status == "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == item.ProductID);
                    if (product != null) product.Stock += item.Quantity;
                }

                if (order.Payment != null)
                {
                    if (order.Payment.PaymentStatus == "Paid")
                        order.Payment.PaymentStatus = "Refund Initiated";
                    else
                        order.Payment.PaymentStatus = "Cancelled";
                }
            }

            if (status == "Delivered" && order.PaymentMethod == "Cash On Delivery" && order.Payment != null)
            {
                order.Payment.PaymentStatus = "Paid";
                order.Payment.PaymentDate = DateTime.Now;
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Order status updated.";
            return RedirectToAction("Orders");
        }

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

            if (feedback == null) return NotFound();
            feedback.IsReadByAdmin = true;
            _context.SaveChanges();
            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAdminMessage(int feedbackId, string message)
        {
            var feedback = await _context.OrderFeedbacks
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null) return NotFound();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int paymentId, string status)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

            if (payment == null) return NotFound();

            if (status == "Paid")
            {
                payment.PaymentStatus = "Paid";
                payment.PaymentDate = DateTime.Now;
            }
            else if (status == "Failed")
            {
                payment.PaymentStatus = "Failed";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Payment status updated successfully.";
            return RedirectToAction("Payments");
        }

        public IActionResult Profile()
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var admin = _context.Admins.FirstOrDefault(x => x.AdminID == adminId);
            return View(admin);
        }

        private bool VerifyPassword(Admin admin, string password)
        {
            if (string.IsNullOrEmpty(admin.PasswordHash) || string.IsNullOrEmpty(password))
                return false;

            var hasher = new PasswordHasher<Admin>();
            var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        private string HashPassword(Admin admin, string password)
        {
            var hasher = new PasswordHasher<Admin>();
            return hasher.HashPassword(admin, password);
        }

        // ==================== VISUAL ANALYTICS (CHART.JS) ====================
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var startDate = DateTime.UtcNow.AddDays(-30);

            // 1. Sales & Revenue Trend (Last 30 Days)
            var salesRaw = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid" && p.PaymentDate.HasValue && p.PaymentDate.Value >= startDate)
                .GroupBy(p => p.PaymentDate!.Value.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            if (!salesRaw.Any())
            {
                // Fallback to real orders by date if no paid payments recorded yet
                salesRaw = await _context.Orders
                    .Where(o => o.OrderDate >= startDate)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync();
            }

            var salesTrend = Enumerable.Range(0, 30)
                .Select(offset => startDate.Date.AddDays(offset))
                .Select(date => new
                {
                    date = date.ToString("dd MMM"),
                    total = salesRaw.FirstOrDefault(s => s.Date == date)?.Total ?? 0
                })
                .ToList();

            // 2. Category Sales & Stock Breakdown from Database
            var categoryBreakdown = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order != null && oi.Order.PaymentStatus == "Paid")
                .GroupBy(oi => oi.Product != null ? oi.Product.Category : "Other")
                .Select(g => new { category = g.Key, total = g.Sum(x => x.ItemTotal) })
                .ToListAsync();

            if (!categoryBreakdown.Any())
            {
                categoryBreakdown = await _context.Products
                    .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "General" : p.Category)
                    .Select(g => new { category = g.Key, total = (decimal)g.Count() })
                    .ToListAsync();
            }

            // 3. Order Status Distribution
            var statusDistribution = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            return Json(new { salesTrend, categoryBreakdown, statusDistribution });
        }

        // ==================== AUDIT LOGS ====================
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }

        // ==================== REVIEWS MODERATION ====================
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = true;
                await _context.SaveChangesAsync();
                await _auditService.LogActionAsync(User.FindFirstValue(ClaimTypes.Email) ?? "Admin", "Approve", "ProductReview", id.ToString(), "Approved product review");
            }
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
                await _auditService.LogActionAsync(User.FindFirstValue(ClaimTypes.Email) ?? "Admin", "Delete", "ProductReview", id.ToString(), "Deleted product review");
            }
            return RedirectToAction(nameof(Reviews));
        }

        // ==================== ENTERPRISE VARIANT MANAGEMENT ====================

        [HttpGet]
        public IActionResult ProductVariants(int id)
        {
            return RedirectToAction("EditProduct", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> SaveVariantAjax(VariantFormDTO dto)
        {
            if (dto.ProductID <= 0)
                return Json(new { success = false, message = "Invalid product ID" });

            var product = await _context.Products.FindAsync(dto.ProductID);
            if (product == null)
                return Json(new { success = false, message = "Product not found" });

            if (!string.IsNullOrEmpty(dto.SKU))
            {
                bool skuExists = await _context.ProductVariants
                    .AnyAsync(v => v.SKU == dto.SKU && v.VariantID != dto.VariantID);
                if (skuExists)
                    return Json(new { success = false, message = $"SKU '{dto.SKU}' is already in use by another variant." });
            }

            if (!string.IsNullOrEmpty(dto.Barcode))
            {
                bool barcodeExists = await _context.ProductVariants
                    .AnyAsync(v => v.Barcode == dto.Barcode && v.VariantID != dto.VariantID);
                if (barcodeExists)
                    return Json(new { success = false, message = $"Barcode '{dto.Barcode}' is already in use." });
            }

            ProductVariant variant;
            if (dto.VariantID > 0)
            {
                variant = await _context.ProductVariants
                    .Include(v => v.Specifications)
                    .FirstOrDefaultAsync(v => v.VariantID == dto.VariantID) 
                    ?? new ProductVariant();
            }
            else
            {
                variant = new ProductVariant
                {
                    ProductID = dto.ProductID,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductVariants.Add(variant);
            }

            variant.VariantName = string.IsNullOrWhiteSpace(dto.VariantName) ? (dto.ColorName ?? dto.Storage ?? "Standard Variant") : dto.VariantName;
            variant.SKU = dto.SKU;
            variant.Barcode = dto.Barcode;
            variant.Price = dto.Price;
            variant.OldPrice = dto.OldPrice;
            variant.Stock = dto.Stock;
            variant.ColorName = dto.ColorName;
            variant.ColorHex = dto.ColorHex;
            variant.Storage = dto.Storage;
            variant.RAM = dto.RAM;
            variant.Processor = dto.Processor;
            variant.ModelNumber = dto.ModelNumber;
            variant.Warranty = dto.Warranty;
            variant.Weight = dto.Weight;
            variant.Length = dto.Length;
            variant.Width = dto.Width;
            variant.Height = dto.Height;
            variant.Description = dto.Description;
            variant.IsActive = dto.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;

            if (dto.IsDefault)
            {
                var otherDefaults = await _context.ProductVariants
                    .Where(v => v.ProductID == dto.ProductID && v.VariantID != dto.VariantID)
                    .ToListAsync();
                foreach (var od in otherDefaults) od.IsDefault = false;
                variant.IsDefault = true;
            }
            else if (!await _context.ProductVariants.AnyAsync(v => v.ProductID == dto.ProductID && v.IsDefault))
            {
                variant.IsDefault = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.SpecificationsRaw))
            {
                _context.VariantSpecifications.RemoveRange(variant.Specifications);
                var lines = dto.SpecificationsRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int sort = 1;
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        var spec = new VariantSpecification
                        {
                            VariantId = variant.VariantID,
                            SpecificationName = parts[0].Trim(),
                            SpecificationValue = string.Join(":", parts.Skip(1)).Trim(),
                            SortOrder = sort++
                        };
                        variant.Specifications.Add(spec);
                    }
                }
            }

            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                int sort = 1;
                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length > 5 * 1024 * 1024) continue;
                    string fileName = await SaveUploadedFilePreservingNameAsync(file, uploadsDir);
                    _context.ProductMediaFiles.Add(new ProductMedia
                    {
                        ProductID = dto.ProductID,
                        VariantID = variant.VariantID,
                        MediaType = "Image",
                        MediaUrl = "/uploads/" + fileName,
                        FileSize = file.Length,
                        SortOrder = sort++
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Variant saved successfully!", variantId = variant.VariantID });
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultVariant(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return Json(new { success = false, message = "Variant not found" });

            var productVariants = await _context.ProductVariants
                .Where(v => v.ProductID == variant.ProductID)
                .ToListAsync();

            foreach (var v in productVariants)
            {
                v.IsDefault = (v.VariantID == variantId);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Default variant updated!" });
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateVariants([FromBody] BulkVariantUpdateDTO dto)
        {
            if (dto.VariantIds == null || !dto.VariantIds.Any())
                return Json(new { success = false, message = "No variants selected for bulk operation." });

            var variants = await _context.ProductVariants
                .Where(v => dto.VariantIds.Contains(v.VariantID))
                .ToListAsync();

            switch (dto.ActionType?.ToLower())
            {
                case "price":
                    if (dto.PriceValue.HasValue)
                    {
                        foreach (var v in variants) v.Price = dto.PriceValue.Value;
                    }
                    break;
                case "stock":
                    if (dto.StockValue.HasValue)
                    {
                        foreach (var v in variants) v.Stock = dto.StockValue.Value;
                    }
                    break;
                case "activate":
                    foreach (var v in variants) v.IsActive = true;
                    break;
                case "deactivate":
                    foreach (var v in variants) v.IsActive = false;
                    break;
                case "delete":
                    _context.ProductVariants.RemoveRange(variants);
                    break;
                default:
                    return Json(new { success = false, message = "Unknown bulk action." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Bulk operation '{dto.ActionType}' executed on {variants.Count} variants!" });
        }


        private static async Task<string> SaveUploadedFilePreservingNameAsync(IFormFile file, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            string originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            string ext = Path.GetExtension(file.FileName).ToLower();

            // Clean invalid filename characters
            string sanitized = string.Concat(originalFileName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "file";

            string fileName = sanitized + ext;
            string fullPath = Path.Combine(targetDirectory, fileName);

            int counter = 1;
            while (System.IO.File.Exists(fullPath))
            {
                fileName = $"{sanitized}_{counter++}{ext}";
                fullPath = Path.Combine(targetDirectory, fileName);
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName;
        }

        // ==================== AI GENERATOR ACTION ====================
        [HttpPost]
        public IActionResult GenerateAIDescription([FromBody] AIDescriptionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductName))
            {
                return Json(new { success = false, message = "Please enter a product name first." });
            }

            string brand = string.IsNullOrWhiteSpace(request.Brand) ? "Premium Brand" : request.Brand;
            string category = string.IsNullOrWhiteSpace(request.Category) ? "Electronics" : request.Category;

            string aiDesc = $"Experience next-gen innovation with the all-new {request.ProductName} by {brand}. Engineered for exceptional performance in {category}, featuring groundbreaking design, all-day battery efficiency, and premium build quality tailored for tech enthusiasts.";

            return Json(new { 
                success = true, 
                description = aiDesc 
            });
        }
    }

    public class AIDescriptionRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Category { get; set; }
    }
}