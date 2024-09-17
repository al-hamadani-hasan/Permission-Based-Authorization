using Microsoft.EntityFrameworkCore;
using Permission_Based_Authorization.Contexts.UserManagement;
using Permission_Based_Authorization.Contexts.UserManagement.Models;
using System.Security.Claims;

namespace Permission_Based_Authorization.Repositories
{
    public interface IRoleRepository
    {
        Task<List<Role>> RolesAsync();

        Task<Role> FindByIdAsync(Guid Id);

        Task<List<Claim>> FindClaimsAsync(Role role);

        Task<bool> RemoveClaimAsync(Role role);

        Task<bool> CreateClaimAsync(Role role, string claimValue);

        Task<bool> IsInRoleAsync(User user, string roleName);

        Task<bool> IsInRoleAsync(User user, Guid roleId);
    }

    public class RoleRepository : IRoleRepository
    {
        private readonly UserManagementDbContext _ctx;

        public RoleRepository(
            UserManagementDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<bool> CreateClaimAsync(Role role, string claimValue)
        {
            var permissions = await _ctx.Permissions
                .ToListAsync();

            if (!permissions.Any()) return default;

            var rolePermissions = new RolePermission();

            foreach (var permission in permissions)
            {
                var tempClaimValue = $"Permissions.{permission.Name}";
                if (tempClaimValue.Equals(claimValue))
                {
                    rolePermissions.RoleId = role.Id;
                    rolePermissions.PermissionId = permission.Id;
                }
            }

            await _ctx.RolePermissions.AddRangeAsync(rolePermissions);

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<Role> FindByIdAsync(Guid Id)
        {
            var role = await _ctx.Roles
                .Where(ops => ops.Id == Id)
                .Select(item => new Role
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .FirstOrDefaultAsync();

            return role ?? new Role();
        }

        public async Task<List<Claim>> FindClaimsAsync(Role role)
        {
            var permissions = await _ctx.RolePermissions
                                    .Where(ops => ops.RoleId == role.Id)
                                    .Include(p => p.Permission)
                                    .AsSplitQuery()
                                    .ToListAsync();


            var claims = new List<Claim>();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", $"Permissions.{permission.Permission.Name}"));
            }

            return claims;
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName)
        {
            var userRoles = await _ctx.UserRoles
                .Where(ops => ops.UserId == user.Id)
                .Include(ops => ops.Role)
                .AsSplitQuery()
                .ToListAsync();

            if (userRoles.Any())
            {
                foreach (var role in userRoles)
                {
                    if (role.Role.Name == roleName)
                        return true;
                }
            }

            return default;
        }

        public async Task<bool> IsInRoleAsync(User user, Guid roleId)
        {
            var userRoles = await _ctx.UserRoles
                .Where(ops => ops.UserId == user.Id)
                .Include(ops => ops.Role)
                .AsSplitQuery()
                .ToListAsync();

            if (userRoles.Any())
            {
                foreach (var role in userRoles)
                {
                    if (role.Role.Id == roleId)
                        return true;
                }
            }

            return default;
        }

        public async Task<bool> RemoveClaimAsync(Role role)
        {
            var rolePermissions = await _ctx.RolePermissions
                .Where(ops => ops.RoleId == role.Id)
                .ToListAsync();

            if (rolePermissions.Count() == 0)
                return true;

            _ctx.RolePermissions.RemoveRange(rolePermissions);

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<List<Role>> RolesAsync()
        {
            var roles = await _ctx.Roles
                .OrderBy(ops => ops.Name)
                .Select(item => new Role
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .AsNoTracking()
                .ToListAsync();

            return roles ?? new List<Role>();
        }
    }
}
