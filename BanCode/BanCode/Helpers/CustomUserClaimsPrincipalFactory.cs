using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BanCode.Models;

namespace BanCode.Helpers
{
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<Guid>>
    {
        public CustomUserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            // 1. Lấy identity mặc định (có sẵn Id, UserName/Email...)
            var identity = await base.GenerateClaimsAsync(user);

            // 2. "Nhét" thêm FullName vào trong thẻ căn cước (Claims)
            identity.AddClaim(new Claim("FullName", user.FullName ?? "Người dùng"));

            return identity;
        }
    }
}