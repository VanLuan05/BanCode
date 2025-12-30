using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class DownloadLog
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProductId { get; set; }

    public DateTime? DownloadedAt { get; set; }

    public string? IpAddress { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
