using Microsoft.EntityFrameworkCore;
using TrendyKart.Models;

namespace TrendyKart.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<CategoryFilterAttribute> CategoryFilterAttributes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<VariantSpecification> VariantSpecifications { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; }
        public DbSet<ProductMedia> ProductMediaFiles { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ShippingSetting> ShippingSettings { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<OrderFeedback> OrderFeedbacks { get; set; }
        public DbSet<FeedbackMessage> FeedbackMessages { get; set; }
        public DbSet<HomeBlock> HomeBlocks { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ServiceablePincode> ServiceablePincodes { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category -> SubCategory relationship
            modelBuilder.Entity<SubCategory>()
                .HasOne(s => s.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(s => s.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            // SubCategory -> Product relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubCategoryID)
                .OnDelete(DeleteBehavior.SetNull);

            // Product -> ProductVariant relationship
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariant -> VariantSpecification relationship
            modelBuilder.Entity<VariantSpecification>()
                .HasOne(vs => vs.Variant)
                .WithMany(v => v.Specifications)
                .HasForeignKey(vs => vs.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariant -> ProductVariantAttribute relationship
            modelBuilder.Entity<ProductVariantAttribute>()
                .HasOne(pva => pva.Variant)
                .WithMany(v => v.VariantAttributes)
                .HasForeignKey(pva => pva.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariantAttribute>()
                .HasOne(pva => pva.AttributeValue)
                .WithMany()
                .HasForeignKey(pva => pva.AttributeValueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product -> ProductMedia relationship
            modelBuilder.Entity<ProductMedia>()
                .HasOne(pm => pm.Product)
                .WithMany(p => p.MediaFiles)
                .HasForeignKey(pm => pm.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariant -> ProductMedia relationship
            modelBuilder.Entity<ProductMedia>()
                .HasOne(pm => pm.Variant)
                .WithMany(v => v.MediaList)
                .HasForeignKey(pm => pm.VariantID)
                .OnDelete(DeleteBehavior.NoAction);

            // Cart Relationships
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Variant)
                .WithMany()
                .HasForeignKey(c => c.VariantID)
                .OnDelete(DeleteBehavior.NoAction);

            // Order Customer
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

            // Order item Relationship
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Variant)
                .WithMany()
                .HasForeignKey(oi => oi.VariantID)
                .OnDelete(DeleteBehavior.NoAction);

            // Payment Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal Precisions
            modelBuilder.Entity<Category>().Property(c => c.GSTPercentage).HasPrecision(18, 2);
            modelBuilder.Entity<SubCategory>().Property(s => s.GSTPercentage).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.OldPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.GSTOverridePercentage).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.Price).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.OldPrice).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.Weight).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.Length).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.Width).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(pv => pv.Height).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.GSTTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.ShippingCharge).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.GSTPercentage).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.GSTAmount).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.ItemTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.DiscountValue).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.MinOrderAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.MaxDiscountCap).HasPrecision(18, 2);
            modelBuilder.Entity<ShippingSetting>().Property(s => s.FreeShippingThreshold).HasPrecision(18, 2);
            modelBuilder.Entity<ShippingSetting>().Property(s => s.FlatShippingRate).HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}