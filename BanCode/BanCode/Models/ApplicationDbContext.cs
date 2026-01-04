using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace BanCode.Models;

// 1. SỬA LỖI CHÍNH: Thay DbContext bằng IdentityDbContext với 8 tham số
public partial class ApplicationDbContext : IdentityDbContext<
    User,
    IdentityRole<Guid>,
    Guid,
    IdentityUserClaim<Guid>,
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Coupon> Coupons { get; set; }
    public virtual DbSet<DownloadLog> DownloadLogs { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<ProductPackage> ProductPackages { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
  

    // Không cần DbSet<User> vì IdentityDbContext đã có sẵn

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string...
        => optionsBuilder.UseSqlServer("Server=LUAN\\SQLEXPRESS;Database=WebProductStore;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 2. QUAN TRỌNG: Gọi base đầu tiên để Identity cấu hình các bảng mặc định
        base.OnModelCreating(modelBuilder);

        // 3. GỘP CẤU HÌNH USER: Đã gộp 2 đoạn code cấu hình User thành 1 để tránh lỗi
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users"); // Map vào bảng users cũ

            // Các cấu hình từ DB cũ (Nên giữ lại để khớp với SQL)
            entity.HasIndex(e => e.Email, "UQ__users__AB6E61640CFEAF2F").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(255).HasColumnName("full_name");
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("customer").HasColumnName("role");

            // Lưu ý: Email, PasswordHash, SecurityStamp... Identity tự lo, không cần cấu hình ở đây nếu không đổi tên cột
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__categori__3213E83FB54B98FB");
            entity.ToTable("categories");
            entity.HasIndex(e => e.Slug, "UQ__categori__32DD1E4C2A8DB217").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasMaxLength(255).HasColumnName("name");
            entity.Property(e => e.Slug).HasMaxLength(255).HasColumnName("slug");
        });
        // --- THÊM ĐOẠN NÀY ĐỂ ĐỒNG BỘ ĐỘ DÀI VỚI SQL ---
        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);
        });
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__coupons__3213E83FA95EA6D6");
            entity.ToTable("coupons");
            entity.HasIndex(e => e.Code, "UQ__coupons__357D4CF9D23DF631").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasMaxLength(50).HasColumnName("code");
            entity.Property(e => e.DiscountPercent).HasColumnName("discount_percent");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
        });

        modelBuilder.Entity<DownloadLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__download__3213E83FA54FCCF7");
            entity.ToTable("download_logs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DownloadedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("downloaded_at");
            entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.DownloadLogs)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_download_product");

            entity.HasOne(d => d.User).WithMany(p => p.DownloadLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_download_user");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__orders__3213E83FE595B15B");
            entity.ToTable("orders");
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())").HasColumnName("id");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50).HasColumnName("payment_method");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending").HasColumnName("status");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)").HasColumnName("total_amount");
            entity.Property(e => e.TransactionId).HasMaxLength(255).HasColumnName("transaction_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_user");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__order_it__3213E83F7E1DEBC5");
            entity.ToTable("order_items");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.PriceAtPurchase).HasColumnType("decimal(12, 2)").HasColumnName("price_at_purchase");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_order");

            entity.HasOne(d => d.Package).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_package");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_product");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__products__3213E83F3A547510");
            entity.ToTable("products");
            entity.HasIndex(e => e.Slug, "UQ__products__32DD1E4C527C641F").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())").HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.Property(e => e.DemoUrl).HasMaxLength(500).HasColumnName("demo_url");
            entity.Property(e => e.IsFeatured).HasDefaultValue(false).HasColumnName("is_featured");
            entity.Property(e => e.ShortDescription).HasMaxLength(500).HasColumnName("short_description");
            entity.Property(e => e.Slug).HasMaxLength(255).HasColumnName("slug");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("draft").HasColumnName("status");
            entity.Property(e => e.TechStack).HasColumnName("tech_stack");
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500).HasColumnName("thumbnail_url");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.Property(e => e.VideoUrl).HasMaxLength(500).HasColumnName("video_url");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_category");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__product___3213E83FC27FCFAA");
            entity.ToTable("product_images");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0).HasColumnName("display_order");
            entity.Property(e => e.ImageUrl).HasMaxLength(500).HasColumnName("image_url");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_product_images_product");
        });

        modelBuilder.Entity<ProductPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__product___3213E83F80226DBE");
            entity.ToTable("product_packages");
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())").HasColumnName("id");
            entity.Property(e => e.Features).HasColumnName("features");
            entity.Property(e => e.FileUrl).HasMaxLength(500).HasColumnName("file_url");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.PackageType).HasMaxLength(20).HasColumnName("package_type");
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)").HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.SalePrice).HasColumnType("decimal(12, 2)").HasColumnName("sale_price");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductPackages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_packages_product");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__reviews__3213E83F92E1A53D");
            entity.ToTable("reviews");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reviews_product");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reviews_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}