using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class Coupon
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int? DiscountPercent { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool? IsActive { get; set; }
}
