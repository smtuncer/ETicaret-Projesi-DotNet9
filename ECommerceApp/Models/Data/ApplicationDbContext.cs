using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Models.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Decimal Precision
        modelBuilder.Entity<Coupon>().Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Coupon>().Property(p => p.MinCartAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>().Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderItem>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentNotification>().Property(p => p.Amount).HasColumnType("decimal(18,2)");

        // Prevent Multiple Cascade Paths for PaymentNotification
        modelBuilder.Entity<PaymentNotification>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PaymentNotification>()
        .HasOne(p => p.Order)
        .WithMany(o => o.PaymentNotifications)
        .HasForeignKey(p => p.OrderId)
        .OnDelete(DeleteBehavior.NoAction);

        // Prevent Multiple Cascade Paths for ReturnRequest
        modelBuilder.Entity<ReturnRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReturnRequest>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReturnRequest>()
            .HasOne(r => r.OrderItem)
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
    public DbSet<SiteContent> SiteContents { get; set; }
    public DbSet<New> News { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Faq> Faqs { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<BlogComment> BlogComments { get; set; }
    public DbSet<MailSetting> MailSettings { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<EmailCampaign> EmailCampaigns { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<SeoSetting> SeoSettings { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ReturnRequest> ReturnRequests { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<PaymentNotification> PaymentNotifications { get; set; }
    public DbSet<Slider> Sliders { get; set; }
    public DbSet<SiteSettings> SiteSettings { get; set; }
    public DbSet<ProductComment> ProductComments { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
}
