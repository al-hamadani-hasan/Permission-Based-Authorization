using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Permission_Based_Authorization.Contexts.UserManagement;
using Permission_Based_Authorization.Contexts.UserManagement.Models;
using System.Security.Claims;

namespace Permission_Based_Authorization.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<bool> SignInAsync(HttpContext httpContext, User user, bool isPersistent = false);

        Task<bool> SignOutAsync(HttpContext httpContext);
    }

    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly UserManagementDbContext _ctx;

        public AuthenticationRepository(
            UserManagementDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<bool> SignInAsync(
            HttpContext httpContext, 
            User user, 
            bool isPersistent = false)
        {
            string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            var _user = _ctx.Users
                .Where(u => u.Id == user.Id)
                .Include(ur => ur!.UserRoles)
                    .ThenInclude(r => r.Role)
                    .ThenInclude(rp => rp.RolePermissions)
                    .ThenInclude(p => p.Permission)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            // Generate Claims from DbEntity
            List<Claim> claims = await FindClaimsAsync(_user);

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, authenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                // AllowRefresh = <bool>,
                // Refreshing the authentication session should be allowed.
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
                // The time at which the authentication ticket expires. A 
                // value set here overrides the ExpireTimeSpan option of 
                // CookieAuthenticationOptions set with AddCookie.
                IsPersistent = isPersistent,
                // Whether the authentication session is persisted across 
                // multiple requests. Required when setting the 
                // ExpireTimeSpan option of CookieAuthenticationOptions 
                // set with AddCookie. Also required when setting 
                // ExpiresUtc.
                // IssuedUtc = <DateTimeOffset>,
                // The time at which the authentication ticket was issued.
                RedirectUri = "/"
                // The full path or absolute URI to be used as an http 
                // redirect response value.
            };

            await httpContext.SignInAsync(
                authenticationScheme,
                claimsPrincipal,
                authProperties);

            return true;
        }

        public async Task<bool> SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return true;
        }

        private async Task<List<Claim>> FindClaimsAsync(Task<User?> user)
        {
            var givenName = user.Result!.FullName;

            var roles = await _ctx.UserRoles
                .Where(ops => ops.UserId == user.Result.Id)
                .Select(r => r.Role)
                .ToListAsync();

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Result!.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Result!.Username),
                new Claim(ClaimTypes.GivenName, givenName),
                new Claim(ClaimTypes.Email, user.Result!.Email!)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var permissions = new List<RolePermission>();

            foreach (var role in roles)
            {
                var tempPermissions = await _ctx.RolePermissions
                    .Where(ops => ops.RoleId == role.Id)
                    .Include(p => p.Permission)
                    .ToListAsync();

                permissions.AddRange(tempPermissions);
            }

            foreach (var permission in permissions.DistinctBy(p => p.Permission.Name))
            {
                claims.Add(new Claim("Permission", $"Permissions.{permission.Permission.Name}"));
            }

            return claims;
        }
    }
}
