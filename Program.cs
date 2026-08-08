using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using TrendyKart.Data;
using TrendyKart.Services;
using TrendyKart.Models;

var builder = WebApplication.CreateBuilder(args);

#region 🔹 DATABASE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region 🔹 MVC
builder.Services.AddControllersWithViews();
#endregion

#region 🔹 SMTP & E-COMMERCE SERVICES CONFIG
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IGSTCalculatorService, GSTCalculatorService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IRazorpayService, RazorpayService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddScoped<IAuditService, AuditService>();

#region 🔹 RATE LIMITING & SECURITY CONTROLS
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("StrictLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 15;
        opt.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("SearchLimiter", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 30;
    });
});
#endregion
#endregion

#region 🔹 AUTHENTICATION
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Google signs the user into this temporary scheme first; GoogleResponse
    // then reads it and issues the real application cookie.
    options.DefaultSignInScheme = "External";
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddCookie("External", options =>
{
    // Short-lived cookie used only during the Google round-trip.
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // Lax (not Strict) so the cookie survives the redirect back from Google.
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                       ?? "YOUR_GOOGLE_CLIENT_ID";

    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                           ?? "YOUR_GOOGLE_CLIENT_SECRET";

    // Persist the Google identity into the temporary "External" cookie.
    options.SignInScheme = "External";
});
#endregion

#region 🔹 AUTHORIZATION
builder.Services.AddAuthorization();
#endregion

#region 🔹 SESSION
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
#endregion

builder.Services.AddHttpContextAccessor();

#region 🔹 BUILD APP
var app = builder.Build();
#endregion

#region 🔹 MIDDLEWARE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region 🔹 ROUTES
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
#endregion


//  ADMIN & SYSTEM SEEDING
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch
    {
        // Database connection or existence handled
    }

    if (!context.Admins.Any())
    {
        var admin = new Admin
        {
            FullName = "Super Admin",
            Email = "trendykart.app@gmail.com"
        };

        var hasher = new PasswordHasher<Admin>();
        admin.PasswordHash = hasher.HashPassword(admin, "123456");

        context.Admins.Add(admin);
        context.SaveChanges();
    }

    // Call Real Demo Products & Categories Seeder
    DbSeeder.SeedAsync(context).GetAwaiter().GetResult();

    //  DEFAULT HOMEPAGE CONTENT
    // Seeds the homepage blocks once so the storefront looks complete out of the box.
    // Images start empty (a placeholder shows) until an admin uploads real ones.
    if (!context.HomeBlocks.Any())
    {
        context.HomeBlocks.AddRange(
            // ---- Hero ----
            new HomeBlock
            {
                Section = "Hero",
                Slug = "hero",
                Eyebrow = "Pro.Beyond.",
                Title = "iPhone 14 Pro",
                Subtitle = "Created to change everything for the better. For everyone.",
                ButtonText = "Shop Now",
                LinkUrl = "/Product/Index",
                ImageUrl = "/images/demo/iphone-hero.png",
                Theme = "Dark",
                SortOrder = 0
            },

            // ---- Feature tiles (the bento grid) ----
            new HomeBlock { Section = "FeatureTile", Title = "Playstation 5", Subtitle = "Incredibly powerful CPUs, GPUs, and an SSD with integrated I/O will redefine your PlayStation experience.", ImageUrl = "/images/demo/playstation-5.png", Size = "Large", Theme = "Light", SortOrder = 0 },
            new HomeBlock { Section = "FeatureTile", Title = "Macbook Air", Subtitle = "The new 15-inch MacBook Air makes room for more of what you love with a spacious Liquid Retina display.", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/macbook-air.png", Size = "Large", Theme = "Gray", SortOrder = 1 },
            new HomeBlock { Section = "FeatureTile", Title = "Apple AirPods Max", Subtitle = "Computational audio. Listen, it's powerful", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/airpods-max.png", Size = "Small", Theme = "Light", SortOrder = 2 },
            new HomeBlock { Section = "FeatureTile", Title = "Apple Vision Pro", Subtitle = "An immersive way to experience entertainment", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/vision-pro.png", Size = "Small", Theme = "Dark", SortOrder = 3 },

            // ---- Category chips (Subtitle holds the Font Awesome icon class) ----
            new HomeBlock { Section = "CategoryChip", Title = "Phones", Subtitle = "fa-mobile-screen", LinkUrl = "/Product/Index?category=Phones", SortOrder = 0 },
            new HomeBlock { Section = "CategoryChip", Title = "Smart Watches", Subtitle = "fa-clock", LinkUrl = "/Product/Index?category=Smart Watches", SortOrder = 1 },
            new HomeBlock { Section = "CategoryChip", Title = "Cameras", Subtitle = "fa-camera", LinkUrl = "/Product/Index?category=Cameras", SortOrder = 2 },
            new HomeBlock { Section = "CategoryChip", Title = "Headphones", Subtitle = "fa-headphones", LinkUrl = "/Product/Index?category=Headphones", SortOrder = 3 },
            new HomeBlock { Section = "CategoryChip", Title = "Computers", Subtitle = "fa-laptop", LinkUrl = "/Product/Index?category=Computers", SortOrder = 4 },
            new HomeBlock { Section = "CategoryChip", Title = "Gaming", Subtitle = "fa-gamepad", LinkUrl = "/Product/Index?category=Gaming", SortOrder = 5 },

            // ---- Popular product tiles ----
            new HomeBlock { Section = "PopularTile", Title = "Popular Products", Subtitle = "iPad, iPhone and more classic Apple picks.", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/phone.jpg", SortOrder = 0 },
            new HomeBlock { Section = "PopularTile", Title = "Ipad Pro", Subtitle = "iPad combines a magnificent display, performance and portability.", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/computer.svg", SortOrder = 1 },
            new HomeBlock { Section = "PopularTile", Title = "Samsung Galaxy", Subtitle = "Galaxy combines a magnificent display, performance and portability.", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/phone.jpg", SortOrder = 2 },
            new HomeBlock { Section = "PopularTile", Title = "Macbook Pro", Subtitle = "Mac combines a magnificent display, performance and portability.", ButtonText = "Shop Now", LinkUrl = "/Product/Index", ImageUrl = "/images/demo/computer.svg", SortOrder = 3 },

            // ---- Sale banner ----
            new HomeBlock
            {
                Section = "SaleBanner",
                Slug = "summer-sale",
                Title = "Big Summer Sale",
                Subtitle = "Commodo fames vitae vitae leo mauris in. Eu consequat.",
                ButtonText = "Shop Now",
                LinkUrl = "/Product/Index",
                Theme = "Dark",
                SortOrder = 0
            }
        );
        context.SaveChanges();
    }

    //  SAMPLE PRODUCTS
    // Seeds a handful of products so the homepage grids look complete.
    // Admin can edit/delete these and upload real images later.
    if (!context.Products.Any())
    {
        var now = DateTime.UtcNow;
        int i = 0;
        // helper so "New Arrival" (ordered by CreatedAt) has a sensible order
        DateTime Stamp() => now.AddSeconds(i++);

        var seedProducts = new List<Product>
        {
            new Product { Name = "Apple iPhone 14 Pro 128GB Deep Purple", Description = "Pro camera system and Dynamic Island.", Category = "Phones", Price = 900, Stock = 20, IsFeatured = true, CreatedAt = Stamp() },
            new Product { Name = "Apple iPhone 14 Pro 512GB Gold", Description = "A16 Bionic. Always-On display.", Category = "Phones", Price = 1437, OldPrice = 1599, Stock = 12, CreatedAt = Stamp() },
            new Product { Name = "Samsung Galaxy S23 Ultra", Description = "200MP camera and built-in S Pen.", Category = "Phones", Price = 1199, Stock = 18, IsBestseller = true, CreatedAt = Stamp() },
            new Product { Name = "Galaxy Z Fold5 Phantom Black", Description = "Foldable display, flagship performance.", Category = "Phones", Price = 1799, OldPrice = 1999, Stock = 8, IsFeatured = true, CreatedAt = Stamp() },

            new Product { Name = "Apple Watch Series 9 41mm Starlight", Description = "Smarter, brighter, more powerful.", Category = "Smart Watches", Price = 399, Stock = 30, IsBestseller = true, CreatedAt = Stamp() },
            new Product { Name = "Samsung Galaxy Watch6 Classic 47mm", Description = "Rotating bezel, advanced health tracking.", Category = "Smart Watches", Price = 369, Stock = 22, CreatedAt = Stamp() },

            new Product { Name = "Blackmagic Pocket Cinema Camera 6K", Description = "Super 35 sensor, cinematic footage.", Category = "Cameras", Price = 2535, Stock = 6, IsFeatured = true, CreatedAt = Stamp() },
            new Product { Name = "Sony Alpha A7 IV", Description = "33MP full-frame hybrid camera.", Category = "Cameras", Price = 2499, OldPrice = 2699, Stock = 9, CreatedAt = Stamp() },

            new Product { Name = "AirPods Max Silver", Description = "High-fidelity audio with spatial sound.", Category = "Headphones", Price = 549, OldPrice = 599, Stock = 25, IsBestseller = true, CreatedAt = Stamp() },
            new Product { Name = "Sony WH-1000XM5", Description = "Industry-leading noise cancellation.", Category = "Headphones", Price = 399, Stock = 28, CreatedAt = Stamp() },
            new Product { Name = "Galaxy Buds FE Graphite", Description = "Immersive sound and active noise cancelling.", Category = "Headphones", Price = 99, Stock = 40, CreatedAt = Stamp() },

            new Product { Name = "MacBook Air 15\" M2", Description = "Spacious Liquid Retina display, all-day battery.", Category = "Computers", Price = 1299, Stock = 15, IsFeatured = true, CreatedAt = Stamp() },
            new Product { Name = "MacBook Pro 16\" M3 Pro", Description = "Blazing performance for pros.", Category = "Computers", Price = 2499, Stock = 10, IsBestseller = true, CreatedAt = Stamp() },
            new Product { Name = "iPad Pro 12.9\"", Description = "Magnificent display, portability and power.", Category = "Computers", Price = 999, OldPrice = 1099, Stock = 16, CreatedAt = Stamp() },

            new Product { Name = "PlayStation 5 Console", Description = "Lightning-fast SSD and stunning games.", Category = "Gaming", Price = 499, Stock = 14, IsBestseller = true, IsFeatured = true, CreatedAt = Stamp() },
            new Product { Name = "Xbox Series X", Description = "12 teraflops of raw power, 4K gaming.", Category = "Gaming", Price = 499, OldPrice = 549, Stock = 13, CreatedAt = Stamp() },
            new Product { Name = "Apple Vision Pro", Description = "An immersive way to experience entertainment.", Category = "Gaming", Price = 3499, Stock = 5, IsFeatured = true, CreatedAt = Stamp() }
        };

        // give each product its category's demo image (admin can replace later)
        var demoImages = new Dictionary<string, string>
        {
            ["Phones"] = "/images/demo/phone.jpg",
            ["Smart Watches"] = "/images/demo/watch.svg",
            ["Cameras"] = "/images/demo/camera.svg",
            ["Headphones"] = "/images/demo/headphones.svg",
            ["Computers"] = "/images/demo/computer.svg",
            ["Gaming"] = "/images/demo/gaming.svg"
        };
        foreach (var p in seedProducts)
        {
            if (demoImages.TryGetValue(p.Category, out var img))
                p.ImageUrl = img;
        }

        context.Products.AddRange(seedProducts);
        context.SaveChanges();
    }
}

app.Run();