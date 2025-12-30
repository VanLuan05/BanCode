using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class OrderItem
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid PackageId { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductPackage Package { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
