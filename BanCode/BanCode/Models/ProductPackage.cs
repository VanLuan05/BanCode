using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class ProductPackage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string PackageType { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public string? Features { get; set; }

    public string? FileUrl { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;
}
