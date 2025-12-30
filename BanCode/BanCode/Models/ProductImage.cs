using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class ProductImage
{
    public int Id { get; set; }

    public Guid ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int? DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
