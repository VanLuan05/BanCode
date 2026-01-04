using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BanCode.Models;

public partial class User : IdentityUser<Guid>
{
  

    public string FullName { get; set; } = null!;
    
    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<DownloadLog> DownloadLogs { get; set; } = new List<DownloadLog>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
