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
            var identity = await base.GenerateClaimsAsync(user);

            // 1. Thêm FullName (như cũ)
            identity.AddClaim(new Claim("FullName", user.FullName ?? "Người dùng"));

            // 2. QUAN TRỌNG: Lấy Role từ bảng users và gán vào Claim chuẩn của Identity
            // Lưu ý: user.Role của bạn đang là "admin" hoặc "customer"
            if (!string.IsNullOrEmpty(user.Role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
            }

            return identity;
        }
    }
}