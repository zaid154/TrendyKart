using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TrendyKart.Data;
using TrendyKart.Models;
using TrendyKart.ViewModels;

namespace TrendyKart.Controllers
{
    [AllowAnonymous]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private async Task LoadCartItems()
        {
            int customerId = GetCurrentCustomerId();
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.CustomerID == customerId)
                .ToListAsync();
            ViewBag.CartItems = cartItems;
        }

        public async Task<IActionResult> Index(
            string search, 
            string category, 
            int? categoryId,
            int? subCategoryId,
            decimal? minPrice, 
            decimal? maxPrice,
            string brand,
            string sort, 
            int page = 1)
        {
            int pageSize = 9;

            var categoriesList = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();

            ViewBag.CategoryTree = categoriesList;

            if (string.IsNullOrEmpty(category) && !categoryId.HasValue && !subCategoryId.HasValue && !string.IsNullOrEmpty(search))
            {
                var matchedCategory = categoriesList.FirstOrDefault(c => 
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
                if (matchedCategory != null)
                {
                    categoryId = matchedCategory.CategoryID;
                    category = matchedCategory.Name;
                }
                else
                {
                    var matchedSub = categoriesList.SelectMany(c => c.SubCategories)
                        .FirstOrDefault(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
                    if (matchedSub != null)
                    {
                        subCategoryId = matchedSub.SubCategoryID;
                        category = matchedSub.Name;
                    }
                }
            }

            var query = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.MediaFiles)
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .AsQueryable();

            if (subCategoryId.HasValue && subCategoryId.Value > 0)
            {
                query = query.Where(p => p.SubCategoryID == subCategoryId.Value);
            }
            else if (categoryId.HasValue && categoryId.Value > 0)
            {
                var subIds = await _context.SubCategories.Where(s => s.CategoryID == categoryId.Value).Select(s => s.SubCategoryID).ToListAsync();
                query = query.Where(p => (p.CategoryID == categoryId.Value) || (p.SubCategoryID.HasValue && subIds.Contains(p.SubCategoryID.Value)));
            }
            else if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category || (p.SubCategory != null && p.SubCategory.Name == category));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Category.Contains(search) ||
                    (p.Brand != null && p.Brand.Contains(search)) ||
                    p.Variants.Any(v => 
                        (v.SKU != null && v.SKU.Contains(search)) ||
                        (v.Barcode != null && v.Barcode.Contains(search)) ||
                        (v.ColorName != null && v.ColorName.Contains(search)) ||
                        (v.Storage != null && v.Storage.Contains(search)) ||
                        (v.RAM != null && v.RAM.Contains(search)) ||
                        (v.Processor != null && v.Processor.Contains(search))));
            }

            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand == brand);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value || p.Variants.Any(v => v.Price >= minPrice.Value));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value || p.Variants.Any(v => v.Price <= maxPrice.Value));
            }

            var activeAttributeFilters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                if (key.StartsWith("attr_") && !string.IsNullOrEmpty(Request.Query[key]))
                {
                    string attrName = key.Substring(5);
                    string attrVal = Request.Query[key].ToString();
                    activeAttributeFilters[attrName] = attrVal;

                    query = query.Where(p =>
                        (p.SpecificationsJson != null && p.SpecificationsJson.Contains(attrVal)) ||
                        p.Variants.Any(v => 
                            (v.AttributesJson != null && v.AttributesJson.Contains(attrVal)) || 
                            (v.SpecificationsJson != null && v.SpecificationsJson.Contains(attrVal)) ||
                            v.ColorName == attrVal ||
                            v.Storage == attrVal ||
                            v.RAM == attrVal));
                }
            }

            List<CategoryFilterAttribute> filterAttributes = new List<CategoryFilterAttribute>();
            if (subCategoryId.HasValue)
            {
                filterAttributes = await _context.CategoryFilterAttributes
                    .Where(f => f.SubCategoryID == subCategoryId.Value)
                    .ToListAsync();
            }
            else if (categoryId.HasValue)
            {
                filterAttributes = await _context.CategoryFilterAttributes
                    .Where(f => f.CategoryID == categoryId.Value || (f.SubCategory != null && f.SubCategory.CategoryID == categoryId.Value))
                    .ToListAsync();
            }
            else
            {
                filterAttributes = await _context.CategoryFilterAttributes.Take(5).ToListAsync();
            }

            ViewBag.FilterAttributes = filterAttributes;
            ViewBag.ActiveAttributeFilters = activeAttributeFilters;

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "name" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.ProductID),
            };

            int totalProducts = await query.CountAsync();
            var pagedProducts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSubCategoryId = subCategoryId;
            ViewBag.CurrentBrand = brand;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentSort = sort;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.CurrentPage = page;
            
            ViewBag.AvailableBrands = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .ToListAsync();

            await LoadCartItems();
            return View(pagedProducts);
        }

        public async Task<IActionResult> Details(int id, int? variant, int? variantId)
        {
            int? targetVariantId = variant ?? variantId;

            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.MediaList)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Specifications)
                .Include(p => p.MediaFiles)
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
                return NotFound();

            // Selected Variant determination
            ProductVariant? activeVariant = null;
            if (targetVariantId.HasValue && targetVariantId.Value > 0)
            {
                activeVariant = product.Variants.FirstOrDefault(v => v.VariantID == targetVariantId.Value && v.IsActive);
            }
            if (activeVariant == null)
            {
                activeVariant = product.Variants.FirstOrDefault(v => v.IsDefault && v.IsActive) 
                                ?? product.Variants.FirstOrDefault(v => v.IsActive)
                                ?? product.Variants.FirstOrDefault();
            }

            ViewBag.ActiveVariant = activeVariant;

            // Extract unique variant attribute matrices (Colors, Storage, RAM)
            ViewBag.AvailableColors = product.Variants
                .Where(v => v.IsActive && !string.IsNullOrEmpty(v.ColorName))
                .Select(v => new { ColorName = v.ColorName!, ColorHex = v.ColorHex ?? "#000000" })
                .GroupBy(x => x.ColorName)
                .Select(g => g.First())
                .ToList();

            ViewBag.AvailableStorage = product.Variants
                .Where(v => v.IsActive && !string.IsNullOrEmpty(v.Storage))
                .Select(v => v.Storage!)
                .Distinct()
                .ToList();

            ViewBag.AvailableRAM = product.Variants
                .Where(v => v.IsActive && !string.IsNullOrEmpty(v.RAM))
                .Select(v => v.RAM!)
                .Distinct()
                .ToList();

            int customerId = GetCurrentCustomerId();

            var reviews = await _context.ProductReviews
                .Include(r => r.Customer)
                .Where(r => r.ProductID == id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : product.Rating;
            ViewBag.TotalReviewCount = reviews.Count;

            ViewBag.IsWishlisted = await _context.Wishlists
                .AnyAsync(w => w.ProductID == id && w.CustomerID == customerId);

            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.ProductID != id && (p.Category == product.Category || p.SubCategoryID == product.SubCategoryID))
                .Take(4)
                .ToListAsync();

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetVariantDetailsJson(int variantId)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.MediaList)
                .Include(v => v.Specifications)
                .FirstOrDefaultAsync(v => v.VariantID == variantId);

            if (variant == null)
                return NotFound(new { message = "Variant not found" });

            decimal effectivePrice = variant.Price;
            decimal? effectiveOldPrice = variant.OldPrice ?? (variant.Product?.OldPrice);

            int savingsPct = 0;
            decimal savingsAmt = 0;
            if (effectiveOldPrice.HasValue && effectiveOldPrice.Value > effectivePrice)
            {
                savingsAmt = effectiveOldPrice.Value - effectivePrice;
                savingsPct = (int)Math.Round((savingsAmt / effectiveOldPrice.Value) * 100);
            }

            int availStock = variant.AvailableStock;
            bool inStock = availStock > 0;
            bool lowStock = inStock && availStock <= 5;
            string stockText = !inStock ? "Out of Stock" : (lowStock ? $"Only {availStock} left in stock - order soon!" : "In Stock");

            // Images
            var imagesList = new List<string>();
            if (!string.IsNullOrEmpty(variant.ImageUrl))
            {
                imagesList.Add(variant.ImageUrl);
            }
            if (variant.MediaList != null && variant.MediaList.Any())
            {
                var extraImgs = variant.MediaList.OrderBy(m => m.SortOrder).Select(m => m.MediaUrl).ToList();
                foreach (var img in extraImgs)
                {
                    if (!imagesList.Contains(img)) imagesList.Add(img);
                }
            }
            if (!imagesList.Any() && variant.Product != null && !string.IsNullOrEmpty(variant.Product.ImageUrl))
            {
                imagesList.Add(variant.Product.ImageUrl);
            }

            // Specifications
            var specsDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(variant.SpecificationsJson))
            {
                var pairs = variant.SpecificationsJson.Split(new[] { '|', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var parts = pair.Split(':');
                    if (parts.Length >= 2)
                    {
                        var k = parts[0].Trim();
                        var v = string.Join(":", parts.Skip(1)).Trim();
                        if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
                        {
                            specsDict[k] = v;
                        }
                    }
                }
            }

            if (variant.Specifications != null && variant.Specifications.Any())
            {
                foreach (var s in variant.Specifications.OrderBy(s => s.SortOrder))
                {
                    specsDict[s.SpecificationName] = s.SpecificationValue;
                }
            }
            if (!specsDict.ContainsKey("Color") && !string.IsNullOrEmpty(variant.ColorName)) specsDict["Color"] = variant.ColorName;
            if (!specsDict.ContainsKey("Storage") && !string.IsNullOrEmpty(variant.Storage)) specsDict["Storage"] = variant.Storage;
            if (!specsDict.ContainsKey("RAM") && !string.IsNullOrEmpty(variant.RAM)) specsDict["RAM"] = variant.RAM;
            if (!specsDict.ContainsKey("Processor") && !string.IsNullOrEmpty(variant.Processor)) specsDict["Processor"] = variant.Processor;
            if (!specsDict.ContainsKey("Warranty") && !string.IsNullOrEmpty(variant.Warranty)) specsDict["Warranty"] = variant.Warranty;

            var dto = new VariantDetailJsonDTO
            {
                VariantId = variant.VariantID,
                ProductId = variant.ProductID,
                VariantName = string.IsNullOrEmpty(variant.VariantName) ? (variant.Product?.Name ?? "") : variant.VariantName,
                SKU = variant.SKU ?? variant.Product?.SKU ?? $"SKU-PV-{variant.VariantID}",
                Barcode = variant.Barcode,
                Price = effectivePrice,
                OldPrice = effectiveOldPrice,
                FormattedPrice = $"₹{effectivePrice:N2}",
                FormattedOldPrice = effectiveOldPrice.HasValue ? $"₹{effectiveOldPrice.Value:N2}" : null,
                SavingsPercentage = savingsPct,
                SavingsAmount = savingsAmt,
                Stock = availStock,
                InStock = inStock,
                LowStock = lowStock,
                StockStatusText = stockText,
                ColorName = variant.ColorName,
                ColorHex = variant.ColorHex,
                Storage = variant.Storage,
                RAM = variant.RAM,
                Processor = variant.Processor,
                ModelNumber = variant.ModelNumber,
                Warranty = variant.Warranty,
                WeightText = variant.Weight.HasValue ? $"{variant.Weight.Value} kg" : null,
                DimensionsText = (variant.Length.HasValue && variant.Width.HasValue && variant.Height.HasValue) 
                    ? $"{variant.Length.Value} x {variant.Width.Value} x {variant.Height.Value} cm" 
                    : null,
                Description = string.IsNullOrWhiteSpace(variant.LongDescription) ? (variant.ShortDescription ?? variant.Description ?? variant.Product?.Description) : variant.LongDescription,
                Images = imagesList,
                Specifications = specsDict
            };

            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> FindVariantByAttributes(int productId, string? color, string? storage, string? ram)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductID == productId && v.IsActive)
                .ToListAsync();

            var match = variants.FirstOrDefault(v => 
                (string.IsNullOrEmpty(color) || string.Equals(v.ColorName, color, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(storage) || string.Equals(v.Storage, storage, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(ram) || string.Equals(v.RAM, ram, StringComparison.OrdinalIgnoreCase)));

            if (match != null)
                return Json(new { success = true, variantId = match.VariantID });

            // Fallback match to closest matching storage or color
            match = variants.FirstOrDefault(v => 
                (string.IsNullOrEmpty(storage) || string.Equals(v.Storage, storage, StringComparison.OrdinalIgnoreCase)) ||
                (string.IsNullOrEmpty(color) || string.Equals(v.ColorName, color, StringComparison.OrdinalIgnoreCase))) 
                ?? variants.FirstOrDefault();

            if (match != null)
                return Json(new { success = true, variantId = match.VariantID });

            return Json(new { success = false, message = "Variant combination not found" });
        }

        [HttpGet]
        [EnableRateLimiting("SearchLimiter")]
        public async Task<IActionResult> SearchSuggestions(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(new List<object>());

            var matches = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.Name.Contains(q) || 
                            (p.Brand != null && p.Brand.Contains(q)) || 
                            p.Category.Contains(q) ||
                            p.Variants.Any(v => (v.SKU != null && v.SKU.Contains(q)) || (v.ColorName != null && v.ColorName.Contains(q))))
                .Take(5)
                .Select(p => new {
                    id = p.ProductID,
                    name = p.Name,
                    category = p.Category,
                    price = p.Variants.Any() ? p.Variants.Min(v => v.Price) : p.Price,
                    image = p.ImageUrl
                })
                .ToListAsync();

            return Json(matches);
        }

        [HttpGet]
        public async Task<IActionResult> CheckPincode(string pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode) || pincode.Length < 6)
                return Json(new { isServiceable = false, message = "Please enter a valid 6-digit pin code." });

            var pin = await _context.ServiceablePincodes
                .FirstOrDefaultAsync(p => p.Pincode == pincode.Trim() && p.IsActive);

            if (pin != null)
            {
                var estDate = DateTime.Now.AddDays(pin.EstimatedDays).ToString("dddd, dd MMMM");
                return Json(new {
                    isServiceable = true,
                    city = pin.City,
                    state = pin.State,
                    estimatedDays = pin.EstimatedDays,
                    estimatedDate = estDate,
                    isCODAvailable = pin.IsCODAvailable,
                    message = $"Delivery available by {estDate} to {pin.City}, {pin.State}!"
                });
            }

            var defaultEstDate = DateTime.Now.AddDays(4).ToString("dddd, dd MMMM");
            return Json(new {
                isServiceable = true,
                city = "Standard Region",
                state = "India",
                estimatedDays = 4,
                estimatedDate = defaultEstDate,
                isCODAvailable = true,
                message = $"Estimated Delivery by {defaultEstDate}."
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string headline, string comment)
        {
            int customerId = GetCurrentCustomerId();
            if (customerId == 0)
                return Json(new { success = false, message = "Please login to submit a review." });

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(headline) || string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Please fill in all review fields correctly." });

            var review = new ProductReview
            {
                ProductID = productId,
                CustomerID = customerId,
                Rating = rating,
                Headline = headline,
                Comment = comment,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);

            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var allRatings = await _context.ProductReviews
                    .Where(r => r.ProductID == productId && r.IsApproved)
                    .Select(r => r.Rating)
                    .ToListAsync();
                allRatings.Add(rating);

                product.TotalReviews = allRatings.Count;
                product.Rating = Math.Round(allRatings.Average(), 1);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thank you! Your review has been published." });
        }

        [HttpGet]
        public async Task<IActionResult> GetVariant(int variantId)
        {
            return await GetVariantDetailsJson(variantId);
        }
    }
}