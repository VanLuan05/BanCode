using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class Product
{
    public Guid Id { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ShortDescription { get; set; }

    public string? Content { get; set; }

    public string? TechStack { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? DemoUrl { get; set; }

    public string? VideoUrl { get; set; }

    public string? Status { get; set; }

    public bool? IsFeatured { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<DownloadLog> DownloadLogs { get; set; } = new List<DownloadLog>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductPackage> ProductPackages { get; set; } = new List<ProductPackage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
