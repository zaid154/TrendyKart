using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Auto-ensure database schema columns exist for Variant System
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='IsActive')
                BEGIN
                    ALTER TABLE Products ADD IsActive bit NOT NULL DEFAULT 1;
                    ALTER TABLE Products ADD ShortDescription nvarchar(max) NULL;
                    ALTER TABLE Products ADD LongDescription nvarchar(max) NULL;
                    ALTER TABLE Products ADD UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
                END

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='Barcode')
                BEGIN
                    ALTER TABLE ProductVariants ADD Barcode nvarchar(50) NULL;
                    ALTER TABLE ProductVariants ADD Weight decimal(18,2) NULL;
                    ALTER TABLE ProductVariants ADD Length decimal(18,2) NULL;
                    ALTER TABLE ProductVariants ADD Width decimal(18,2) NULL;
                    ALTER TABLE ProductVariants ADD Height decimal(18,2) NULL;
                    ALTER TABLE ProductVariants ADD Storage nvarchar(50) NULL;
                    ALTER TABLE ProductVariants ADD RAM nvarchar(50) NULL;
                    ALTER TABLE ProductVariants ADD Processor nvarchar(100) NULL;
                    ALTER TABLE ProductVariants ADD ModelNumber nvarchar(100) NULL;
                    ALTER TABLE ProductVariants ADD Warranty nvarchar(150) NULL;
                    ALTER TABLE ProductVariants ADD Description nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD ShortDescription nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD LongDescription nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD ImageUrl nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD IsActive bit NOT NULL DEFAULT 1;
                    ALTER TABLE ProductVariants ADD ReservedStock int NOT NULL DEFAULT 0;
                    ALTER TABLE ProductVariants ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
                    ALTER TABLE ProductVariants ADD UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
                END
                ELSE IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='ShortDescription')
                BEGIN
                    ALTER TABLE ProductVariants ADD ShortDescription nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD LongDescription nvarchar(max) NULL;
                    ALTER TABLE ProductVariants ADD ImageUrl nvarchar(max) NULL;
                END

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='VariantSpecifications')
                BEGIN
                    CREATE TABLE VariantSpecifications (
                        Id int IDENTITY(1,1) PRIMARY KEY,
                        VariantId int NOT NULL FOREIGN KEY REFERENCES ProductVariants(VariantID) ON DELETE CASCADE,
                        SpecificationName nvarchar(100) NOT NULL,
                        SpecificationValue nvarchar(500) NOT NULL,
                        SortOrder int NOT NULL DEFAULT 0
                    );
                END

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='ProductAttributes')
                BEGIN
                    CREATE TABLE ProductAttributes (
                        Id int IDENTITY(1,1) PRIMARY KEY,
                        Name nvarchar(100) NOT NULL
                    );

                    CREATE TABLE AttributeValues (
                        Id int IDENTITY(1,1) PRIMARY KEY,
                        AttributeId int NOT NULL FOREIGN KEY REFERENCES ProductAttributes(Id) ON DELETE CASCADE,
                        Value nvarchar(100) NOT NULL,
                        ColorHex nvarchar(30) NULL
                    );

                    CREATE TABLE ProductVariantAttributes (
                        Id int IDENTITY(1,1) PRIMARY KEY,
                        VariantId int NOT NULL FOREIGN KEY REFERENCES ProductVariants(VariantID) ON DELETE CASCADE,
                        AttributeValueId int NOT NULL FOREIGN KEY REFERENCES AttributeValues(Id) ON DELETE CASCADE
                    );
                END
                ");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Schema DDL Migration Note: " + ex.Message);
            }

            // -------------------------------------------------------------
            // 1. CLEAR EXISTING SEED DATA FOR A CLEAN STATE
            // -------------------------------------------------------------
            if (await context.Products.AnyAsync() || await context.Customers.AnyAsync())
            {
                context.FeedbackMessages.RemoveRange(context.FeedbackMessages);
                context.OrderFeedbacks.RemoveRange(context.OrderFeedbacks);
                context.Payments.RemoveRange(context.Payments);
                context.Carts.RemoveRange(context.Carts);
                context.OrderItems.RemoveRange(context.OrderItems);
                context.Orders.RemoveRange(context.Orders);
                context.ProductMediaFiles.RemoveRange(context.ProductMediaFiles);
                context.ProductVariants.RemoveRange(context.ProductVariants);
                context.Products.RemoveRange(context.Products);
                context.CategoryFilterAttributes.RemoveRange(context.CategoryFilterAttributes);
                await context.SaveChangesAsync();

                context.SubCategories.RemoveRange(context.SubCategories);
                await context.SaveChangesAsync();

                context.Categories.RemoveRange(context.Categories);
                context.Coupons.RemoveRange(context.Coupons);
                context.SiteSettings.RemoveRange(context.SiteSettings);
                await context.SaveChangesAsync();
            }

            // Helper local function to construct image paths cleanly from /uploads/demo/
            string Img(string filename) => $"/uploads/demo/{filename}";

            // -------------------------------------------------------------
            // 2. CREATE 5 MAIN CATEGORIES & 14 SUB-CATEGORIES
            // -------------------------------------------------------------
            var cat1 = new Category { Name = "Electronics", Slug = "electronics" };
            var cat2 = new Category { Name = "Computers", Slug = "computers" };
            var cat3 = new Category { Name = "Gaming", Slug = "gaming" };
            var cat4 = new Category { Name = "Cameras", Slug = "cameras" };
            var cat5 = new Category { Name = "Accessories", Slug = "accessories" };

            context.Categories.AddRange(cat1, cat2, cat3, cat4, cat5);
            await context.SaveChangesAsync();

            // 14 Sub-Categories
            var sub1_1 = new SubCategory { Name = "Phones", Slug = "phones", CategoryID = cat1.CategoryID };
            var sub1_2 = new SubCategory { Name = "Smart Watches", Slug = "smart-watches", CategoryID = cat1.CategoryID };
            var sub1_3 = new SubCategory { Name = "Headphones", Slug = "headphones", CategoryID = cat1.CategoryID };

            var sub2_1 = new SubCategory { Name = "Laptops & MacBooks", Slug = "laptops-macbooks", CategoryID = cat2.CategoryID };
            var sub2_2 = new SubCategory { Name = "Desktop PCs", Slug = "desktop-pcs", CategoryID = cat2.CategoryID };
            var sub2_3 = new SubCategory { Name = "Tablets & iPads", Slug = "tablets-ipads", CategoryID = cat2.CategoryID };

            var sub3_1 = new SubCategory { Name = "Gaming Consoles", Slug = "gaming-consoles", CategoryID = cat3.CategoryID };
            var sub3_2 = new SubCategory { Name = "VR & Spatial Computing", Slug = "vr-spatial-computing", CategoryID = cat3.CategoryID };
            var sub3_3 = new SubCategory { Name = "Gaming Controllers", Slug = "gaming-controllers", CategoryID = cat3.CategoryID };

            var sub4_1 = new SubCategory { Name = "DSLR & Mirrorless", Slug = "dslr-mirrorless", CategoryID = cat4.CategoryID };
            var sub4_2 = new SubCategory { Name = "Action Cameras & Drones", Slug = "action-cameras-drones", CategoryID = cat4.CategoryID };
            var sub4_3 = new SubCategory { Name = "Camera Lenses", Slug = "camera-lenses", CategoryID = cat4.CategoryID };

            var sub5_1 = new SubCategory { Name = "Smart Home", Slug = "smart-home", CategoryID = cat5.CategoryID };
            var sub5_2 = new SubCategory { Name = "Power Banks & Cables", Slug = "power-banks-cables", CategoryID = cat5.CategoryID };

            context.SubCategories.AddRange(
                sub1_1, sub1_2, sub1_3,
                sub2_1, sub2_2, sub2_3,
                sub3_1, sub3_2, sub3_3,
                sub4_1, sub4_2, sub4_3,
                sub5_1, sub5_2
            );
            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 3. SEED PRODUCTS WITH DEMO-PRODUCTS IMAGES & FULL 5-7 SPECS
            // -------------------------------------------------------------
            var products = new List<Product>
            {
                // 1. iPhone 17 Pro Max (5 VARIANTS)
                new Product
                {
                    Name = "Apple iPhone 17 Pro Max",
                    Slug = "apple-iphone-17-pro-max",
                    Brand = "Apple",
                    Category = "Phones",
                    SubCategoryID = sub1_1.SubCategoryID,
                    Price = 144900,
                    OldPrice = 154900,
                    Stock = 50,
                    Rating = 4.9,
                    TotalReviews = 142,
                    IsFeatured = true,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("iphone-17-pro.png"),
                    Description = "The flagship iPhone 17 Pro Max features forged titanium design, groundbreaking A19 Pro 3nm chip, customizable Action button, and 48MP periscope telephoto camera system.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple A19 Pro (3nm)" },
                        new { Key = "Display", Value = "6.7-inch Super Retina XDR OLED 120Hz" },
                        new { Key = "CPU Cores", Value = "6-Core CPU (2 performance + 4 efficiency)" },
                        new { Key = "Main Camera", Value = "48MP Main + 48MP Ultra Wide + 48MP Telephoto 5x" },
                        new { Key = "Front Camera", Value = "12MP TrueDepth with Autofocus" },
                        new { Key = "Battery Capacity", Value = "4422 mAh with 30W Fast Charging" },
                        new { Key = "Operating System", Value = "iOS 18" },
                        new { Key = "Warranty", Value = "1 Year AppleCare Warranty" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "256GB / 12GB RAM - Titanium Black", SKU = "IP17PM-256-BLK", Price = 144900, OldPrice = 154900, Stock = 15, IsDefault = true, ColorName = "Titanium Black", ColorHex = "#1C1B1B", Storage = "256GB", RAM = "12GB", ImageUrl = "/uploads/products/iphone17_black.png" },
                        new ProductVariant { VariantName = "256GB / 12GB RAM - Titanium White", SKU = "IP17PM-256-WHT", Price = 144900, OldPrice = 154900, Stock = 10, ColorName = "Titanium White", ColorHex = "#F0F0F0", Storage = "256GB", RAM = "12GB", ImageUrl = "/uploads/products/iphone17_white.png" },
                        new ProductVariant { VariantName = "512GB / 16GB RAM - Natural Titanium", SKU = "IP17PM-512-NAT", Price = 164900, OldPrice = 174900, Stock = 12, ColorName = "Natural Titanium", ColorHex = "#B8B5AD", Storage = "512GB", RAM = "16GB", ImageUrl = "/uploads/products/iphone17_natural.png" },
                        new ProductVariant { VariantName = "512GB / 16GB RAM - Titanium Blue", SKU = "IP17PM-512-BLU", Price = 164900, OldPrice = 174900, Stock = 8, ColorName = "Titanium Blue", ColorHex = "#283845", Storage = "512GB", RAM = "16GB", ImageUrl = "/uploads/products/iphone17_blue.png" },
                        new ProductVariant { VariantName = "1TB / 16GB RAM - Titanium Gold", SKU = "IP17PM-1TB-GLD", Price = 184900, OldPrice = 194900, Stock = 5, ColorName = "Titanium Gold", ColorHex = "#E5C158", Storage = "1TB", RAM = "16GB", ImageUrl = "/uploads/products/iphone17_gold.png" }
                    }
                },

                // 2. Apple iPhone 14 Pro (4 VARIANTS)
                new Product
                {
                    Name = "Apple iPhone 14 Pro Max",
                    Slug = "apple-iphone-14-pro-max",
                    Brand = "Apple",
                    Category = "Phones",
                    SubCategoryID = sub1_1.SubCategoryID,
                    Price = 119900,
                    OldPrice = 139900,
                    Stock = 35,
                    Rating = 4.8,
                    TotalReviews = 98,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("Apple iPhone 14 Pro 128GB Deep Purple.webp"),
                    Description = "iPhone 14 Pro featuring Dynamic Island, 48MP Main camera with Quad-Pixel sensor, Always-On Super Retina XDR display and A16 Bionic chip.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple A16 Bionic 6-core CPU" },
                        new { Key = "Display", Value = "6.7-inch Super Retina XDR ProMotion 120Hz" },
                        new { Key = "RAM / Memory", Value = "6GB High Speed LPDDR5" },
                        new { Key = "Main Camera", Value = "48MP Main + 12MP Ultra Wide + 12MP 3x Telephoto" },
                        new { Key = "Front Camera", Value = "12MP TrueDepth Camera" },
                        new { Key = "Battery Capacity", Value = "4323 mAh All-Day Battery" },
                        new { Key = "Operating System", Value = "iOS 17" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "128GB - Deep Purple", SKU = "IP14P-128-PUR", Price = 119900, OldPrice = 139900, Stock = 12, IsDefault = true, ColorName = "Deep Purple", ColorHex = "#5E4B66" },
                        new ProductVariant { VariantName = "256GB - Deep Purple", SKU = "IP14P-256-PUR", Price = 129900, OldPrice = 149900, Stock = 10, ColorName = "Deep Purple", ColorHex = "#5E4B66" },
                        new ProductVariant { VariantName = "512GB - Gold", SKU = "IP14P-512-GLD", Price = 149900, OldPrice = 169900, Stock = 8, ColorName = "Gold", ColorHex = "#F4E5CE" },
                        new ProductVariant { VariantName = "1TB - Space Black", SKU = "IP14P-1TB-BLK", Price = 169900, OldPrice = 189900, Stock = 5, ColorName = "Space Black", ColorHex = "#2E2E30" }
                    }
                },

                // 3. Samsung Galaxy S23 Ultra (5 VARIANTS)
                new Product
                {
                    Name = "Samsung Galaxy S23 Ultra 5G",
                    Slug = "samsung-galaxy-s23-ultra",
                    Brand = "Samsung",
                    Category = "Phones",
                    SubCategoryID = sub1_1.SubCategoryID,
                    Price = 124999,
                    OldPrice = 149999,
                    Stock = 45,
                    Rating = 4.8,
                    TotalReviews = 112,
                    IsFeatured = true,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("Samsung Galaxy S23 Ultra.webp"),
                    Description = "Samsung Galaxy S23 Ultra features an embedded S Pen, 200MP camera sensor with Nightography, and Snapdragon 8 Gen 2 for Galaxy.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Snapdragon 8 Gen 2 for Galaxy" },
                        new { Key = "Display", Value = "6.8-inch Dynamic AMOLED 2X 120Hz" },
                        new { Key = "CPU Cores", Value = "Octa-Core 3.36 GHz" },
                        new { Key = "Main Camera", Value = "200MP + 12MP + 10MP + 10MP Quad Camera" },
                        new { Key = "Front Camera", Value = "12MP Dual Pixel AF" },
                        new { Key = "Battery Capacity", Value = "5000 mAh 45W Super Fast Charging" },
                        new { Key = "Operating System", Value = "Android 14 / One UI 6" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "256GB / 12GB RAM - Phantom Black", SKU = "S23U-256-BLK", Price = 124999, OldPrice = 149999, Stock = 15, IsDefault = true, ColorName = "Phantom Black", ColorHex = "#1C1B1B" },
                        new ProductVariant { VariantName = "256GB / 12GB RAM - Cream", SKU = "S23U-256-CRM", Price = 124999, OldPrice = 149999, Stock = 10, ColorName = "Cream", ColorHex = "#F3EFE0" },
                        new ProductVariant { VariantName = "512GB / 12GB RAM - Green", SKU = "S23U-512-GRN", Price = 139999, OldPrice = 164999, Stock = 10, ColorName = "Green", ColorHex = "#3B4D3C" },
                        new ProductVariant { VariantName = "512GB / 12GB RAM - Lavender", SKU = "S23U-512-LAV", Price = 139999, OldPrice = 164999, Stock = 6, ColorName = "Lavender", ColorHex = "#D0C3DB" },
                        new ProductVariant { VariantName = "1TB / 12GB RAM - Phantom Black", SKU = "S23U-1TB-BLK", Price = 159999, OldPrice = 179999, Stock = 4, ColorName = "Phantom Black", ColorHex = "#1C1B1B" }
                    }
                },

                // 4. Samsung Galaxy Z Fold5 (3 VARIANTS)
                new Product
                {
                    Name = "Samsung Galaxy Z Fold5 5G",
                    Slug = "samsung-galaxy-z-fold5",
                    Brand = "Samsung",
                    Category = "Phones",
                    SubCategoryID = sub1_1.SubCategoryID,
                    Price = 154999,
                    OldPrice = 169999,
                    Stock = 25,
                    Rating = 4.7,
                    TotalReviews = 64,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("Galaxy Z Fold5 Phantom Black.webp"),
                    Description = "7.6-inch main foldable Dynamic AMOLED screen with Flex Hinge, Dual App Multitasking, and Snapdragon 8 Gen 2 processor.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Snapdragon 8 Gen 2 Mobile Platform" },
                        new { Key = "Display", Value = "7.6-inch Foldable Dynamic AMOLED 2X 120Hz" },
                        new { Key = "Cover Display", Value = "6.2-inch Dynamic AMOLED 2X 120Hz" },
                        new { Key = "RAM / Memory", Value = "12GB LPDDR5X RAM" },
                        new { Key = "Main Camera", Value = "50MP Triple Camera System with 3x Optical Zoom" },
                        new { Key = "Battery Capacity", Value = "4400 mAh Dual Battery" },
                        new { Key = "Operating System", Value = "Android 14 Foldable UI" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "256GB / 12GB RAM - Phantom Black", SKU = "ZFOLD5-256-BLK", Price = 154999, OldPrice = 169999, Stock = 10, IsDefault = true, ColorName = "Phantom Black", ColorHex = "#1C1B1B" },
                        new ProductVariant { VariantName = "512GB / 12GB RAM - Icy Blue", SKU = "ZFOLD5-512-BLU", Price = 169999, OldPrice = 184999, Stock = 10, ColorName = "Icy Blue", ColorHex = "#A3C1D4" },
                        new ProductVariant { VariantName = "1TB / 12GB RAM - Cream", SKU = "ZFOLD5-1TB-CRM", Price = 189999, OldPrice = 204999, Stock = 5, ColorName = "Cream", ColorHex = "#F3EFE0" }
                    }
                },

                // 5. Apple AirPods Max (5 VARIANTS)
                new Product
                {
                    Name = "Apple AirPods Max Wireless Headphones",
                    Slug = "apple-airpods-max",
                    Brand = "Apple",
                    Category = "Headphones",
                    SubCategoryID = sub1_3.SubCategoryID,
                    Price = 59900,
                    OldPrice = 64900,
                    Stock = 40,
                    Rating = 4.7,
                    TotalReviews = 98,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("AirPods Max Silver.webp"),
                    Description = "AirPods Max reimagine over-ear headphones. Dynamic driver provides immersive high-fidelity audio with Active Noise Cancellation.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Dual Apple H1 Headphone Chips" },
                        new { Key = "Audio Tech", Value = "Apple-designed Dynamic Driver + Spatial Audio" },
                        new { Key = "Noise Control", Value = "Active Noise Cancellation + Transparency Mode" },
                        new { Key = "Microphones", Value = "9 Microphones Total for ANC & Voice" },
                        new { Key = "Battery Life", Value = "20 Hours Listening Time with ANC" },
                        new { Key = "Weight", Value = "384.8 grams" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "Silver Starlight Aluminium", SKU = "APM-SLV", Price = 59900, OldPrice = 64900, Stock = 10, IsDefault = true, ColorName = "Silver", ColorHex = "#E3E4E5" },
                        new ProductVariant { VariantName = "Space Gray Aluminium", SKU = "APM-GRY", Price = 59900, OldPrice = 64900, Stock = 10, ColorName = "Space Gray", ColorHex = "#53555B" },
                        new ProductVariant { VariantName = "Sky Blue Aluminium", SKU = "APM-BLU", Price = 59900, OldPrice = 64900, Stock = 8, ColorName = "Sky Blue", ColorHex = "#87A9BC" },
                        new ProductVariant { VariantName = "Pink Aluminium", SKU = "APM-PNK", Price = 59900, OldPrice = 64900, Stock = 7, ColorName = "Pink", ColorHex = "#E8B4B8" },
                        new ProductVariant { VariantName = "Green Aluminium", SKU = "APM-GRN", Price = 59900, OldPrice = 64900, Stock = 5, ColorName = "Green", ColorHex = "#A8C3B2" }
                    }
                },

                // 6. Sony WH-1000XM5 (2 VARIANTS)
                new Product
                {
                    Name = "Sony WH-1000XM5 Wireless Headphones",
                    Slug = "sony-wh-1000xm5",
                    Brand = "Sony",
                    Category = "Headphones",
                    SubCategoryID = sub1_3.SubCategoryID,
                    Price = 29990,
                    OldPrice = 34990,
                    Stock = 30,
                    Rating = 4.9,
                    TotalReviews = 184,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("Sony WH-1000XM5.webp"),
                    Description = "Industry-leading noise cancelling wireless headphones with 2 processors, 8 microphones, and Auto NC Optimizer.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "HD Noise Cancelling Processor QN1 + V1" },
                        new { Key = "Driver Unit", Value = "30mm Precision Engineered Driver" },
                        new { Key = "Battery Life", Value = "Up to 30 Hours Playback with Quick Charging" },
                        new { Key = "Microphones", Value = "8 Microphones with AI Precise Voice Pickup" },
                        new { Key = "Connectivity", Value = "Bluetooth 5.2 Multipoint Connection" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "Black Edition", SKU = "XM5-BLK", Price = 29990, OldPrice = 34990, Stock = 20, IsDefault = true, ColorName = "Black", ColorHex = "#1C1B1B" },
                        new ProductVariant { VariantName = "Silver Cream Edition", SKU = "XM5-SLV", Price = 29990, OldPrice = 34990, Stock = 10, ColorName = "Silver Cream", ColorHex = "#EAE6DF" }
                    }
                },

                // 7. Samsung Galaxy Buds3 Pro (2 VARIANTS)
                new Product
                {
                    Name = "Samsung Galaxy Buds3 Pro Wireless Earbuds",
                    Slug = "samsung-galaxy-buds3-pro",
                    Brand = "Samsung",
                    Category = "Headphones",
                    SubCategoryID = sub1_3.SubCategoryID,
                    Price = 19999,
                    OldPrice = 22999,
                    Stock = 35,
                    Rating = 4.8,
                    TotalReviews = 76,
                    FreeShipping = true,
                    ImageUrl = Img("galaxy-buds3-pro.png"),
                    Description = "Next-gen Galaxy Buds3 Pro with Blade Lights, 24-bit Hi-Fi audio sound, Adaptive Noise Control, and Galaxy AI Interpreter integration.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Samsung Custom BES Audio Chip" },
                        new { Key = "Audio Quality", Value = "24-bit / 96kHz Hi-Fi SSC Codec" },
                        new { Key = "Speakers", Value = "2-Way Woofer + Planar Tweeter" },
                        new { Key = "Battery Life", Value = "Up to 30 Hours with Charging Case" },
                        new { Key = "Water Resistance", Value = "IP57 Dust and Water Resistant" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "Silver Metallic", SKU = "BUDS3-SLV", Price = 19999, OldPrice = 22999, Stock = 20, IsDefault = true, ColorName = "Silver", ColorHex = "#A0A0A0" },
                        new ProductVariant { VariantName = "White Edition", SKU = "BUDS3-WHT", Price = 19999, OldPrice = 22999, Stock = 15, ColorName = "White", ColorHex = "#FFFFFF" }
                    }
                },

                // 8. Apple Watch Series 9 (4 VARIANTS)
                new Product
                {
                    Name = "Apple Watch Series 9 GPS",
                    Slug = "apple-watch-series-9",
                    Brand = "Apple",
                    Category = "Smart Watches",
                    SubCategoryID = sub1_2.SubCategoryID,
                    Price = 41900,
                    OldPrice = 44900,
                    Stock = 35,
                    Rating = 4.8,
                    TotalReviews = 65,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("Apple Watch Series 9 41mm Starlight.webp"),
                    Description = "Apple Watch Series 9 helps you stay connected, active, healthy, and safe. Featuring Double Tap gesture control and S9 SiP.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple S9 SiP Dual-Core" },
                        new { Key = "Display", Value = "Always-On Retina OLED 2000 nits" },
                        new { Key = "Screen Size", Value = "41mm / 45mm" },
                        new { Key = "Sensors", Value = "ECG, Blood Oxygen, Temperature Sensing" },
                        new { Key = "Battery Life", Value = "18 Hours All-Day Battery" },
                        new { Key = "Water Resistance", Value = "50m Water Resistant" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "41mm Aluminium - Starlight", SKU = "AW9-41-STL", Price = 41900, OldPrice = 44900, Stock = 10, IsDefault = true, ColorName = "Starlight", ColorHex = "#F0EAE1" },
                        new ProductVariant { VariantName = "41mm Aluminium - Midnight", SKU = "AW9-41-MID", Price = 41900, OldPrice = 44900, Stock = 10, ColorName = "Midnight", ColorHex = "#1E252B" },
                        new ProductVariant { VariantName = "45mm Aluminium - Silver", SKU = "AW9-45-SLV", Price = 44900, OldPrice = 47900, Stock = 10, ColorName = "Silver", ColorHex = "#E0E0E0" },
                        new ProductVariant { VariantName = "45mm Stainless Steel - Graphite", SKU = "AW9-45-SS-GPH", Price = 74900, OldPrice = 79900, Stock = 5, ColorName = "Graphite", ColorHex = "#4A4A4A" }
                    }
                },

                // 9. Samsung Galaxy Watch6 Classic (2 VARIANTS)
                new Product
                {
                    Name = "Samsung Galaxy Watch6 Classic 47mm",
                    Slug = "samsung-galaxy-watch6-classic",
                    Brand = "Samsung",
                    Category = "Smart Watches",
                    SubCategoryID = sub1_2.SubCategoryID,
                    Price = 36999,
                    OldPrice = 40999,
                    Stock = 25,
                    Rating = 4.7,
                    TotalReviews = 52,
                    FreeShipping = true,
                    ImageUrl = Img("Samsung Galaxy Watch6 Classic 47mm.avif"),
                    Description = "Iconic rotating bezel smartwatch with Sapphire Crystal glass, Advanced Sleep Tracking, and BIA Body Composition Sensor.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Exynos W930 Dual-Core 1.4GHz" },
                        new { Key = "Display", Value = "1.5-inch Super AMOLED (480x480) Sapphire Crystal" },
                        new { Key = "RAM / Memory", Value = "2GB RAM + 16GB Storage" },
                        new { Key = "Sensors", Value = "BioActive Sensor (ECG, BIA, HR), Temperature" },
                        new { Key = "Battery Life", Value = "Up to 40 Hours Usage" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "47mm Bluetooth - Black", SKU = "GW6C-47-BLK", Price = 36999, OldPrice = 40999, Stock = 15, IsDefault = true, ColorName = "Black", ColorHex = "#1C1B1B" },
                        new ProductVariant { VariantName = "47mm LTE - Silver", SKU = "GW6C-47-SLV", Price = 41999, OldPrice = 45999, Stock = 10, ColorName = "Silver", ColorHex = "#E0E0E0" }
                    }
                },

                // 10. MacBook Pro 16" M3 Pro (4 VARIANTS)
                new Product
                {
                    Name = "Apple MacBook Pro 16-inch M3 Pro",
                    Slug = "apple-macbook-pro-16",
                    Brand = "Apple",
                    Category = "Computers",
                    SubCategoryID = sub2_1.SubCategoryID,
                    Price = 249900,
                    OldPrice = 269900,
                    Stock = 30,
                    Rating = 4.9,
                    TotalReviews = 42,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("MacBook Pro 16.webp"),
                    Description = "The 16-inch MacBook Pro with M3 Pro chip takes performance further than ever. Liquid Retina XDR display with up to 22 hours battery life.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple M3 Pro 12-Core CPU" },
                        new { Key = "GPU", Value = "18-Core Integrated GPU" },
                        new { Key = "Display", Value = "16.2-inch Liquid Retina XDR (3456 x 2234) 120Hz" },
                        new { Key = "RAM / Memory", Value = "18GB / 36GB / 48GB Unified Memory" },
                        new { Key = "Storage", Value = "512GB / 1TB / 2TB Superfast SSD" },
                        new { Key = "Battery Life", Value = "Up to 22 Hours Apple TV Playback" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "18GB RAM / 512GB SSD - Space Black", SKU = "MBP16-18-512-BLK", Price = 249900, OldPrice = 269900, Stock = 12, IsDefault = true, ColorName = "Space Black", ColorHex = "#2E2E30" },
                        new ProductVariant { VariantName = "18GB RAM / 512GB SSD - Silver", SKU = "MBP16-18-512-SLV", Price = 249900, OldPrice = 269900, Stock = 8, ColorName = "Silver", ColorHex = "#E0E0E0" },
                        new ProductVariant { VariantName = "36GB RAM / 1TB SSD - Space Black", SKU = "MBP16-36-1TB-BLK", Price = 289900, OldPrice = 309900, Stock = 6, ColorName = "Space Black", ColorHex = "#2E2E30" },
                        new ProductVariant { VariantName = "48GB RAM / 2TB SSD - Space Black", SKU = "MBP16-48-2TB-BLK", Price = 349900, OldPrice = 369900, Stock = 4, ColorName = "Space Black", ColorHex = "#2E2E30" }
                    }
                },

                // 11. MacBook Air 15" M2 (3 VARIANTS)
                new Product
                {
                    Name = "Apple MacBook Air 15-inch M2",
                    Slug = "apple-macbook-air-15-m2",
                    Brand = "Apple",
                    Category = "Computers",
                    SubCategoryID = sub2_1.SubCategoryID,
                    Price = 134900,
                    OldPrice = 144900,
                    Stock = 35,
                    Rating = 4.8,
                    TotalReviews = 89,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("Macbook Air.webp"),
                    Description = "Impossibly thin 15-inch Liquid Retina display with Apple M2 chip, silent fanless design and 18 hours battery life.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple M2 8-Core CPU" },
                        new { Key = "GPU", Value = "10-Core Integrated GPU" },
                        new { Key = "Display", Value = "15.3-inch Liquid Retina Display (2880 x 1864)" },
                        new { Key = "RAM / Memory", Value = "8GB / 16GB Unified Memory" },
                        new { Key = "Storage", Value = "256GB / 512GB / 1TB SSD" },
                        new { Key = "Weight", Value = "1.51 kg Ultra Portable" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "8GB RAM / 256GB SSD - Midnight", SKU = "MBA15-8-256-MID", Price = 134900, OldPrice = 144900, Stock = 15, IsDefault = true, ColorName = "Midnight", ColorHex = "#1E252B" },
                        new ProductVariant { VariantName = "8GB RAM / 512GB SSD - Starlight", SKU = "MBA15-8-512-STL", Price = 154900, OldPrice = 164900, Stock = 12, ColorName = "Starlight", ColorHex = "#F0EAE1" },
                        new ProductVariant { VariantName = "16GB RAM / 512GB SSD - Space Gray", SKU = "MBA15-16-512-GRY", Price = 174900, OldPrice = 184900, Stock = 8, ColorName = "Space Gray", ColorHex = "#53555B" }
                    }
                },

                // 12. iPad Pro 12.9" M2 (4 VARIANTS)
                new Product
                {
                    Name = "Apple iPad Pro 12.9-inch M2",
                    Slug = "apple-ipad-pro-12-9",
                    Brand = "Apple",
                    Category = "Computers",
                    SubCategoryID = sub2_3.SubCategoryID,
                    Price = 112900,
                    OldPrice = 122900,
                    Stock = 30,
                    Rating = 4.8,
                    TotalReviews = 53,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("iPad Pro 12.9.jpg"),
                    Description = "Incredibly advanced Liquid Retina XDR display with Apple M2 chip performance and Apple Pencil hover experience.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple M2 8-core CPU" },
                        new { Key = "Display", Value = "12.9-inch Liquid Retina XDR Mini-LED 120Hz" },
                        new { Key = "RAM / Memory", Value = "8GB / 16GB Unified RAM" },
                        new { Key = "Storage", Value = "128GB / 256GB / 512GB / 1TB SSD" },
                        new { Key = "Camera", Value = "12MP Wide + 10MP Ultra Wide + LiDAR Scanner" },
                        new { Key = "Battery Life", Value = "Up to 10 Hours Surfing the Web" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "128GB Wi-Fi - Space Gray", SKU = "IPD-128-WF", Price = 112900, OldPrice = 122900, Stock = 10, IsDefault = true, ColorName = "Space Gray", ColorHex = "#53555B" },
                        new ProductVariant { VariantName = "256GB Wi-Fi - Silver", SKU = "IPD-256-WF", Price = 122900, OldPrice = 132900, Stock = 10, ColorName = "Silver", ColorHex = "#E0E0E0" },
                        new ProductVariant { VariantName = "512GB Wi-Fi + Cellular - Space Gray", SKU = "IPD-512-CEL", Price = 142900, OldPrice = 152900, Stock = 6, ColorName = "Space Gray", ColorHex = "#53555B" },
                        new ProductVariant { VariantName = "1TB Wi-Fi + Cellular - Space Gray", SKU = "IPD-1TB-CEL", Price = 172900, OldPrice = 182900, Stock = 4, ColorName = "Space Gray", ColorHex = "#53555B" }
                    }
                },

                // 13. PlayStation 5 Console (3 VARIANTS)
                new Product
                {
                    Name = "Sony PlayStation 5 Console",
                    Slug = "sony-playstation-5",
                    Brand = "Sony",
                    Category = "Gaming",
                    SubCategoryID = sub3_1.SubCategoryID,
                    Price = 54990,
                    OldPrice = 59990,
                    Stock = 30,
                    Rating = 4.9,
                    TotalReviews = 210,
                    IsFeatured = true,
                    IsBestseller = true,
                    FreeShipping = true,
                    ImageUrl = Img("Playstation 5.webp"),
                    Description = "Experience lightning fast loading with ultra-high speed SSD, deeper immersion with haptic feedback, adaptive triggers and 3D Audio.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Custom AMD Zen 2 8 Cores @ 3.5GHz" },
                        new { Key = "Graphics", Value = "Custom AMD RDNA 2 GPU 10.28 TFLOPS" },
                        new { Key = "Storage", Value = "825GB / 1TB Ultra High Speed NVMe SSD" },
                        new { Key = "Video Output", Value = "4K 120Hz HDR, 8K Output Support" },
                        new { Key = "Audio", Value = "Tempest 3D AudioTech" },
                        new { Key = "Controller", Value = "DualSense Wireless Controller Included" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "Disc Edition Console (825GB)", SKU = "PS5-DISC", Price = 54990, OldPrice = 59990, Stock = 15, IsDefault = true, ColorName = "White", ColorHex = "#FFFFFF" },
                        new ProductVariant { VariantName = "Digital Edition Console (825GB)", SKU = "PS5-DIGITAL", Price = 44990, OldPrice = 49990, Stock = 10, ColorName = "White", ColorHex = "#FFFFFF" },
                        new ProductVariant { VariantName = "Slim 1TB Disc Edition Bundle", SKU = "PS5-SLIM-1TB", Price = 59990, OldPrice = 64990, Stock = 5, ColorName = "White", ColorHex = "#FFFFFF" }
                    }
                },

                // 14. Apple Vision Pro (3 VARIANTS)
                new Product
                {
                    Name = "Apple Vision Pro Spatial Computer",
                    Slug = "apple-vision-pro",
                    Brand = "Apple",
                    Category = "Gaming",
                    SubCategoryID = sub3_2.SubCategoryID,
                    Price = 349900,
                    OldPrice = 369900,
                    Stock = 15,
                    Rating = 4.7,
                    TotalReviews = 29,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("Apple Vision Pro.jpg"),
                    Description = "Apple Vision Pro seamlessly blends digital content with your physical space. Controlled by your eyes, hands, and voice.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "Apple M2 + R1 Dual Architecture" },
                        new { Key = "Display", Value = "Micro-OLED 23 Million Pixels 3D Display" },
                        new { Key = "RAM / Memory", Value = "16GB Unified Memory" },
                        new { Key = "Storage", Value = "256GB / 512GB / 1TB High-Speed Storage" },
                        new { Key = "Camera", Value = "3D Main Camera System + Eye Tracking Sensors" },
                        new { Key = "Battery Life", Value = "Up to 2 Hours General Use with External Battery" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "256GB Storage", SKU = "VP-256", Price = 349900, OldPrice = 369900, Stock = 7, IsDefault = true, ColorName = "Silver", ColorHex = "#E0E0E0" },
                        new ProductVariant { VariantName = "512GB Storage", SKU = "VP-512", Price = 369900, OldPrice = 389900, Stock = 5, ColorName = "Silver", ColorHex = "#E0E0E0" },
                        new ProductVariant { VariantName = "1TB Storage", SKU = "VP-1TB", Price = 389900, OldPrice = 409900, Stock = 3, ColorName = "Silver", ColorHex = "#E0E0E0" }
                    }
                },

                // 15. Sony Alpha A7 IV Camera (3 VARIANTS)
                new Product
                {
                    Name = "Sony Alpha A7 IV Full-Frame Camera",
                    Slug = "sony-alpha-a7-iv",
                    Brand = "Sony",
                    Category = "Cameras",
                    SubCategoryID = sub4_1.SubCategoryID,
                    Price = 224990,
                    OldPrice = 244990,
                    Stock = 20,
                    Rating = 4.8,
                    TotalReviews = 34,
                    IsFeatured = true,
                    FreeShipping = true,
                    ImageUrl = Img("Sony Alpha A7 IV.webp"),
                    Description = "33MP Full-Frame Exmor R CMOS Sensor, 4K 60p 10-bit 4:2:2 video recording with BIONZ XR processing engine.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Processor", Value = "BIONZ XR Image Processor" },
                        new { Key = "Sensor", Value = "33MP Full-Frame Exmor R Back-Illuminated CMOS" },
                        new { Key = "Video Resolution", Value = "4K 60p in Super 35 / 4K 30p 7K Oversampled" },
                        new { Key = "Autofocus", Value = "759 Phase-Detection AF Points with Real-Time Eye AF" },
                        new { Key = "Stabilization", Value = "5-Axis In-Body Optical Image Stabilization" },
                        new { Key = "Battery Life", Value = "580 Shots Per Charge (NP-FZ100)" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "Body Only", SKU = "A7M4-BODY", Price = 224990, OldPrice = 244990, Stock = 10, IsDefault = true, ColorName = "Black", ColorHex = "#000000" },
                        new ProductVariant { VariantName = "With 28-70mm Lens Kit", SKU = "A7M4-2870", Price = 244990, OldPrice = 264990, Stock = 6, ColorName = "Black", ColorHex = "#000000" },
                        new ProductVariant { VariantName = "With 24-105mm f/4 G Lens Kit", SKU = "A7M4-24105", Price = 304990, OldPrice = 324990, Stock = 4, ColorName = "Black", ColorHex = "#000000" }
                    }
                },

                // 16. Blackmagic Pocket Cinema Camera 6K (2 VARIANTS)
                new Product
                {
                    Name = "Blackmagic Pocket Cinema Camera 6K Pro",
                    Slug = "blackmagic-pocket-cinema-camera-6k",
                    Brand = "Blackmagic",
                    Category = "Cameras",
                    SubCategoryID = sub4_1.SubCategoryID,
                    Price = 219990,
                    OldPrice = 239990,
                    Stock = 15,
                    Rating = 4.9,
                    TotalReviews = 28,
                    FreeShipping = true,
                    ImageUrl = Img("Blackmagic Pocket Cinema Camera 6K.webp"),
                    Description = "Super 35 HDR sensor with 6144 x 3456 resolution, EF lens mount, built in motorized ND filters, and 1500 nit tilting LCD screen.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Sensor", Value = "Super 35 HDR Sensor (23.10mm x 12.99mm)" },
                        new { Key = "Video Resolution", Value = "6K 50fps / 6K 2.4:1 60fps / 4K 60fps" },
                        new { Key = "Dynamic Range", Value = "13 Stops with Dual Native ISO up to 25,600" },
                        new { Key = "ND Filters", Value = "Built-in 2, 4, 6 Stop Motorized IR ND Filters" },
                        new { Key = "Screen", Value = "5-inch HDR LCD Touchscreen 1500 nits" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "EF Mount Body Only", SKU = "BMPCC-6K-BODY", Price = 219990, OldPrice = 239990, Stock = 10, IsDefault = true, ColorName = "Black", ColorHex = "#000000" },
                        new ProductVariant { VariantName = "With EVF Viewfinder Bundle", SKU = "BMPCC-6K-EVF", Price = 249990, OldPrice = 269990, Stock = 5, ColorName = "Black", ColorHex = "#000000" }
                    }
                },

                // 17. Samsung Sound Tower Party Speaker (2 VARIANTS)
                new Product
                {
                    Name = "Samsung Sound Tower High Power Audio",
                    Slug = "samsung-sound-tower-audio",
                    Brand = "Samsung",
                    Category = "Accessories",
                    SubCategoryID = sub5_1.SubCategoryID,
                    Price = 34990,
                    OldPrice = 39990,
                    Stock = 20,
                    Rating = 4.6,
                    TotalReviews = 41,
                    FreeShipping = true,
                    ImageUrl = Img("sound-tower.jpg"),
                    Description = "Bi-directional sound with 1500 Watts high power output, built-in woofer, LED party lights, and splash resistant top panel.",
                    SpecificationsJson = JsonSerializer.Serialize(new[]
                    {
                        new { Key = "Power Output", Value = "1500 Watts Peak Bi-Directional Sound" },
                        new { Key = "Speakers", Value = "Built-in 10-inch Subwoofer + Dual Tweeters" },
                        new { Key = "Lighting", Value = "Party Lights Mode with DJ Effect" },
                        new { Key = "Connectivity", Value = "Bluetooth Multi-Connection + USB Input" },
                        new { Key = "Water Resistance", Value = "IPX4 Splash Resistant Panel" }
                    }),
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { VariantName = "1500W Standard Party Speaker", SKU = "ST1500-BLK", Price = 34990, OldPrice = 39990, Stock = 12, IsDefault = true, ColorName = "Black", ColorHex = "#000000" },
                        new ProductVariant { VariantName = "1500W + Dual Wireless Mic Bundle", SKU = "ST1500-MIC", Price = 39990, OldPrice = 44990, Stock = 8, ColorName = "Black", ColorHex = "#000000" }
                    }
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Add Product Media Files for 4 vertical thumbnails gallery from real files
            foreach (var p in products)
            {
                context.ProductMediaFiles.AddRange(
                    new ProductMedia { ProductID = p.ProductID, MediaType = "Image", MediaUrl = p.ImageUrl, SortOrder = 1 },
                    new ProductMedia { ProductID = p.ProductID, MediaType = "Image", MediaUrl = Img("iphone-17-pro.png"), SortOrder = 2 },
                    new ProductMedia { ProductID = p.ProductID, MediaType = "Image", MediaUrl = Img("Samsung Galaxy S23 Ultra.webp"), SortOrder = 3 },
                    new ProductMedia { ProductID = p.ProductID, MediaType = "Image", MediaUrl = Img("Apple Vision Pro.jpg"), SortOrder = 4 }
                );
            }
            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 4. SEED CATEGORY FILTER ATTRIBUTES FOR SHOP SIDEBAR ACCORDIONS
            // -------------------------------------------------------------
            var attrs = new List<CategoryFilterAttribute>
            {
                new CategoryFilterAttribute { AttributeName = "Brand", AttributeType = "Checkbox", SubCategoryID = sub1_1.SubCategoryID, OptionsJson = JsonSerializer.Serialize(new[] { "Apple", "Samsung", "Sony", "Asus", "GoPro", "Blackmagic" }) },
                new CategoryFilterAttribute { AttributeName = "Battery capacity", AttributeType = "Checkbox", SubCategoryID = sub1_1.SubCategoryID, OptionsJson = JsonSerializer.Serialize(new[] { "4000-4500 mAh", "4500-5000 mAh", "5000+ mAh" }) },
                new CategoryFilterAttribute { AttributeName = "Screen type", AttributeType = "Checkbox", SubCategoryID = sub1_1.SubCategoryID, OptionsJson = JsonSerializer.Serialize(new[] { "OLED", "Dynamic AMOLED 2X", "Super Retina XDR", "Mini-LED" }) },
                new CategoryFilterAttribute { AttributeName = "Built-in memory", AttributeType = "Checkbox", SubCategoryID = sub1_1.SubCategoryID, OptionsJson = JsonSerializer.Serialize(new[] { "128GB", "256GB", "512GB", "1TB" }) },
                new CategoryFilterAttribute { AttributeName = "RAM / Memory", AttributeType = "Checkbox", SubCategoryID = sub2_1.SubCategoryID, OptionsJson = JsonSerializer.Serialize(new[] { "12GB", "16GB", "18GB", "32GB", "64GB" }) }
            };

            context.CategoryFilterAttributes.AddRange(attrs);

            // Seed Coupons
            var coupon1 = new Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10, EndDate = DateTime.Now.AddYears(1), IsActive = true };
            var coupon2 = new Coupon { Code = "CYBER500", DiscountType = "Flat", DiscountValue = 500, EndDate = DateTime.Now.AddYears(1), IsActive = true };
            context.Coupons.AddRange(coupon1, coupon2);

            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 5. SEED ADMIN ACCOUNT & CUSTOMERS (USERS)
            // -------------------------------------------------------------
            if (!await context.Admins.AnyAsync(a => a.Email == "admin@trendykart.com"))
            {
                var adminHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Admin>();
                var adminUser = new Admin
                {
                    FullName = "Mohd Zaid",
                    Email = "admin@trendykart.com"
                };
                adminUser.PasswordHash = adminHasher.HashPassword(adminUser, "Admin@123");
                context.Admins.Add(adminUser);
            }

            var custHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>();

            var cust1 = new Customer
            {
                FullName = "Mohd Zaid",
                Email = "zaid154@gmail.com",
                Phone = "+91 9876543210",
                DefaultStreet = "Tech Park, Sector 62",
                DefaultCity = "Noida",
                DefaultState = "Uttar Pradesh",
                DefaultPincode = "201301",
                DefaultCountry = "India",
                IsEmailVerified = true,
                IsBlocked = false
            };
            cust1.PasswordHash = custHasher.HashPassword(cust1, "Customer@123");

            var cust2 = new Customer
            {
                FullName = "Rahul Sharma",
                Email = "rahul.sharma@example.com",
                Phone = "+91 9812345678",
                DefaultStreet = "MG Road, Cyber City",
                DefaultCity = "Bangalore",
                DefaultState = "Karnataka",
                DefaultPincode = "560001",
                DefaultCountry = "India",
                IsEmailVerified = true,
                IsBlocked = false
            };
            cust2.PasswordHash = custHasher.HashPassword(cust2, "Customer@123");

            var cust3 = new Customer
            {
                FullName = "Priya Patel",
                Email = "priya.patel@example.com",
                Phone = "+91 9711223344",
                DefaultStreet = "SG Highway, Satellite",
                DefaultCity = "Ahmedabad",
                DefaultState = "Gujarat",
                DefaultPincode = "380015",
                DefaultCountry = "India",
                IsEmailVerified = true,
                IsBlocked = false
            };
            cust3.PasswordHash = custHasher.HashPassword(cust3, "Customer@123");

            var cust4 = new Customer
            {
                FullName = "Ananya Verma",
                Email = "ananya.v@example.com",
                Phone = "+91 9654321876",
                DefaultStreet = "Park Street",
                DefaultCity = "Kolkata",
                DefaultState = "West Bengal",
                DefaultPincode = "700016",
                DefaultCountry = "India",
                IsEmailVerified = true,
                IsBlocked = false
            };
            cust4.PasswordHash = custHasher.HashPassword(cust4, "Customer@123");

            context.Customers.AddRange(cust1, cust2, cust3, cust4);
            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 6. SEED ORDERS, ORDER ITEMS & PAYMENTS
            // -------------------------------------------------------------
            var p1 = products.FirstOrDefault(p => p.Slug == "apple-iphone-17-pro-max") ?? products.First();
            var p2 = products.FirstOrDefault(p => p.Slug == "macbook-pro-16-m3-max") ?? products.Skip(1).First();
            var p3 = products.FirstOrDefault(p => p.Slug == "sony-playstation-5-slim") ?? products.Skip(2).First();
            var p4 = products.FirstOrDefault(p => p.Slug == "apple-airpods-max-usb-c") ?? products.Skip(3).First();

            var order1 = new Order
            {
                OrderNumber = "ORD-2026-1001",
                CustomerID = cust1.CustomerID,
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = "Delivered",
                PaymentStatus = "Paid",
                PaymentMethod = "Razorpay",
                SubTotal = p1.Price,
                ShippingCharge = 0,
                TotalAmount = p1.Price,
                GSTTotal = Math.Round(p1.Price * 0.18m, 2),
                ShippingAddress = cust1.DefaultStreet ?? "Noida Sector 62",
                City = cust1.DefaultCity ?? "Noida",
                State = cust1.DefaultState ?? "Uttar Pradesh",
                Pincode = cust1.DefaultPincode ?? "201301",
                Country = cust1.DefaultCountry ?? "India",
                Phone = cust1.Phone,
                Email = cust1.Email,
                RazorpayOrderId = "order_Noida1001_rzp"
            };

            var order2 = new Order
            {
                OrderNumber = "ORD-2026-1002",
                CustomerID = cust2.CustomerID,
                OrderDate = DateTime.UtcNow.AddDays(-3),
                Status = "Shipped",
                PaymentStatus = "Paid",
                PaymentMethod = "Razorpay",
                SubTotal = p2.Price,
                ShippingCharge = 0,
                TotalAmount = p2.Price,
                GSTTotal = Math.Round(p2.Price * 0.18m, 2),
                ShippingAddress = cust2.DefaultStreet ?? "MG Road",
                City = cust2.DefaultCity ?? "Bangalore",
                State = cust2.DefaultState ?? "Karnataka",
                Pincode = cust2.DefaultPincode ?? "560001",
                Country = cust2.DefaultCountry ?? "India",
                Phone = cust2.Phone,
                Email = cust2.Email,
                RazorpayOrderId = "order_Blr1002_rzp"
            };

            var order3 = new Order
            {
                OrderNumber = "ORD-2026-1003",
                CustomerID = cust3.CustomerID,
                OrderDate = DateTime.UtcNow.AddDays(-1),
                Status = "Processing",
                PaymentStatus = "Paid",
                PaymentMethod = "Razorpay",
                SubTotal = p3.Price,
                ShippingCharge = 0,
                TotalAmount = p3.Price,
                GSTTotal = Math.Round(p3.Price * 0.18m, 2),
                ShippingAddress = cust3.DefaultStreet ?? "SG Highway",
                City = cust3.DefaultCity ?? "Ahmedabad",
                State = cust3.DefaultState ?? "Gujarat",
                Pincode = cust3.DefaultPincode ?? "380015",
                Country = cust3.DefaultCountry ?? "India",
                Phone = cust3.Phone,
                Email = cust3.Email,
                RazorpayOrderId = "order_Ahm1003_rzp"
            };

            var order4 = new Order
            {
                OrderNumber = "ORD-2026-1004",
                CustomerID = cust4.CustomerID,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = "Cash on Delivery",
                SubTotal = p4.Price,
                ShippingCharge = 0,
                TotalAmount = p4.Price,
                GSTTotal = Math.Round(p4.Price * 0.18m, 2),
                ShippingAddress = cust4.DefaultStreet ?? "Park Street",
                City = cust4.DefaultCity ?? "Kolkata",
                State = cust4.DefaultState ?? "West Bengal",
                Pincode = cust4.DefaultPincode ?? "700016",
                Country = cust4.DefaultCountry ?? "India",
                Phone = cust4.Phone,
                Email = cust4.Email
            };

            context.Orders.AddRange(order1, order2, order3, order4);
            await context.SaveChangesAsync();

            // Order Items
            var item1 = new OrderItem { OrderID = order1.OrderID, ProductID = p1.ProductID, ProductName = p1.Name, UnitPrice = p1.Price, Quantity = 1, ItemTotal = p1.Price, ImageUrl = p1.ImageUrl, GSTPercentage = 18, GSTAmount = p1.Price * 0.18m };
            var item2 = new OrderItem { OrderID = order2.OrderID, ProductID = p2.ProductID, ProductName = p2.Name, UnitPrice = p2.Price, Quantity = 1, ItemTotal = p2.Price, ImageUrl = p2.ImageUrl, GSTPercentage = 18, GSTAmount = p2.Price * 0.18m };
            var item3 = new OrderItem { OrderID = order3.OrderID, ProductID = p3.ProductID, ProductName = p3.Name, UnitPrice = p3.Price, Quantity = 1, ItemTotal = p3.Price, ImageUrl = p3.ImageUrl, GSTPercentage = 18, GSTAmount = p3.Price * 0.18m };
            var item4 = new OrderItem { OrderID = order4.OrderID, ProductID = p4.ProductID, ProductName = p4.Name, UnitPrice = p4.Price, Quantity = 1, ItemTotal = p4.Price, ImageUrl = p4.ImageUrl, GSTPercentage = 18, GSTAmount = p4.Price * 0.18m };

            context.OrderItems.AddRange(item1, item2, item3, item4);

            // Payments
            var pay1 = new Payment { OrderID = order1.OrderID, Amount = order1.TotalAmount, PaymentMethod = "Razorpay", PaymentStatus = "Success", RazorpayPaymentId = "pay_Noida1001_rzp", RazorpayOrderId = order1.RazorpayOrderId, PaymentDate = DateTime.UtcNow.AddDays(-5) };
            var pay2 = new Payment { OrderID = order2.OrderID, Amount = order2.TotalAmount, PaymentMethod = "Razorpay", PaymentStatus = "Success", RazorpayPaymentId = "pay_Blr1002_rzp", RazorpayOrderId = order2.RazorpayOrderId, PaymentDate = DateTime.UtcNow.AddDays(-3) };
            var pay3 = new Payment { OrderID = order3.OrderID, Amount = order3.TotalAmount, PaymentMethod = "Razorpay", PaymentStatus = "Success", RazorpayPaymentId = "pay_Ahm1003_rzp", RazorpayOrderId = order3.RazorpayOrderId, PaymentDate = DateTime.UtcNow.AddDays(-1) };

            context.Payments.AddRange(pay1, pay2, pay3);
            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 7. SEED ORDER FEEDBACK (PRODUCT REVIEWS & RATINGS)
            // -------------------------------------------------------------
            var fb1 = new OrderFeedback
            {
                OrderId = order1.OrderID,
                CustomerId = cust1.CustomerID,
                Rating = 5,
                Comment = "The iPhone 17 Pro Max display and camera quality are incredible! Fast shipping by TrendyKart.",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                IsReadByAdmin = false
            };

            var fb2 = new OrderFeedback
            {
                OrderId = order2.OrderID,
                CustomerId = cust2.CustomerID,
                Rating = 5,
                Comment = "M3 Max MacBook Pro is a beast for software engineering and video editing. Authentic product with full Apple warranty.",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsReadByAdmin = false
            };

            context.OrderFeedbacks.AddRange(fb1, fb2);
            await context.SaveChangesAsync();

            // -------------------------------------------------------------
            // 8. SEED HELP DESK / CONTACT FEEDBACK MESSAGES
            // -------------------------------------------------------------
            var msg1 = new FeedbackMessage
            {
                FeedbackId = fb1.Id,
                SenderRole = "Customer",
                Message = "Hi TrendyKart team, how do I register AppleCare+ warranty for my newly purchased iPhone?",
                SentAt = DateTime.UtcNow.AddDays(-2)
            };

            var msg2 = new FeedbackMessage
            {
                FeedbackId = fb2.Id,
                SenderRole = "Customer",
                Message = "Hello, we want to buy 10 units of MacBook Air for our tech startup team. Can you provide a corporate discount coupon?",
                SentAt = DateTime.UtcNow.AddDays(-1)
            };

            context.FeedbackMessages.AddRange(msg1, msg2);

            await context.SaveChangesAsync();
        }
    }
}
