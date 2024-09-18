using Microsoft.EntityFrameworkCore;
using Permission_Based_Authorization.Contexts.UserManagement;
using Permission_Based_Authorization.Contexts.UserManagement.Models;
using Permission_Based_Authorization.Models;
using Permission_Based_Authorization.Services;
using System.Security.Claims;

namespace Permission_Based_Authorization.Repositories
{
    public interface IUserRepository
    {
        Task<bool> CreateAsync(AddUserViewModel model);

        Task<bool> ChangeStatusByIdAsync(Guid Id);

        Task<User> FindByIdAsync(Guid Id);

        Task<User> CurrentUserAsync(HttpContext httpContext);

        Task<List<User>> ExceptUserByIdAsync(Guid Id);

        Task<bool> FindByUsernameAsync(string username);

        Task<List<Role>> RolesAsync(User user);

        Task<bool> UpdateUserRoleAsync(User user, Guid Id);

        Task<bool> RemoveFromRolesAsync(User user, List<Role> roles);

        Task<bool> AddToRolesAsync(User user, List<Role> roles);

        Task<User> MembershipByIdAsync(Guid Id);

        Task<User> ValidationAsync(string username, string password);
    }

    public class UserRepository : IUserRepository
    {
        private readonly UserManagementDbContext _ctx;
        private readonly IHasherService _hasher;

        public UserRepository(
            UserManagementDbContext ctx,
            IHasherService hasher)
        {
            _ctx = ctx;
            _hasher = hasher;
        }

        public async Task<bool> AddToRolesAsync(User user, List<Role> roles)
        {
            var temp = new List<UserRole>();

            foreach (var role in roles)
            {
                temp.Add(new UserRole
                {
                    RoleId = role.Id,
                    UserId = user.Id,
                });
            }

            await _ctx.UserRoles.AddRangeAsync(temp);

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<bool> ChangeStatusByIdAsync(Guid Id)
        {
            var user = await _ctx.Users
                .Where(x => x.Id == Id)
                .FirstOrDefaultAsync();

            if (user == null)
                return default;

            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                user.AccessFailedCount = 0;
            }
            else
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = DateTime.UtcNow;
                user.AccessFailedCount = 3;
            }

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<bool> CreateAsync(AddUserViewModel model)
        {
            var salt = _hasher.Salt();
            var PasswordHash = _hasher.Hasher(model.Password!, salt);

            var roles = await _ctx.Roles.ToListAsync();
            if (!roles.Any()) return default;

            Guid roleId = Guid.NewGuid();

            if (roles.Any(ops => ops.Name == "Editor"))
                roleId = roles.Where(o => o.Name == "Editor").FirstOrDefault()!.Id;

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = model.FullName!.Trim(),
                Username = model.Username!.Trim(),
                NormalizedUsername = model.Username.Trim().ToUpper().Normalize(),
                Email = model.Email!.Trim(),
                NormalizedEmail = model.Email.Trim().ToUpper().Normalize(),
                PasswordHash = PasswordHash,
                Salt = salt,
                AccessFailedCount = 3,
                AccessPrivilege = model.AccessPrivilege!
            };

            var createdUser = await _ctx.Users.AddAsync(user);
            if (createdUser == null) return default;

            await _ctx.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = createdUser.Entity.Id,
                RoleId = roleId,
            };

            var createdUserRole = await _ctx.UserRoles.AddAsync(userRole);
            if (createdUserRole == null) return default;

            await _ctx.SaveChangesAsync();

            return true;
        }

        public async Task<User> CurrentUserAsync(HttpContext httpContext)
        {
            var nameIdentifier = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userId = Guid.Parse(nameIdentifier!);

            var user = await _ctx.Users
                .Where(x => x.Id == userId)
                .Select(item => new User
                {
                    Id = item.Id,
                    Email = item.Email!,
                    FullName = item.FullName,
                    Username = item.Username
                })
                .FirstOrDefaultAsync();

            return user ?? new User();
        }

        public async Task<List<User>> ExceptUserByIdAsync(Guid Id)
        {
            var ctx_user = await _ctx.Users
                .Where(ops => ops.Id != Id)
                .Include(ur => ur!.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(ops => ops.FullName)
                .AsSplitQuery()
                .ToListAsync();

            var user = new List<User>();

            foreach (var item in ctx_user)
            {
                var membership = await MembershipByIdAsync(item.Id);

                user.Add(new User
                {
                    Id = item.Id,
                    FullName = item.FullName,
                    Username = item.Username,
                    Email = item.Email!,
                    EmailConfirmed = item.EmailConfirmed,
                    LockoutEnabled = item.LockoutEnabled,
                    AccessPrivilege = membership.AccessPrivilege,
                });
            }

            return user;
        }

        public async Task<User> FindByIdAsync(Guid Id)
        {
            var user = await _ctx.Users
                .Where(x => x.Id == Id)
                .Select(item => new User
                {
                    Id = item!.Id,
                    Email = item!.Email!,
                    FullName = item!.FullName,
                    Username = item!.Username,
                })
                .FirstOrDefaultAsync();

            return user ?? new User();
        }

        public async Task<bool> FindByUsernameAsync(string username)
        {
            return await _ctx.Users.AnyAsync(x => x.Username == username);
        }

        public async Task<User> MembershipByIdAsync(Guid Id)
        {
            var membership = await _ctx.Users
                .Where(x => x.Id == Id)
                .Select(item => new User
                {
                    AccessPrivilege = item.AccessPrivilege
                })
                .FirstOrDefaultAsync();

            return membership ?? new User();
        }

        public async Task<bool> RemoveFromRolesAsync(User user, List<Role> roles)
        {
            var temp = new List<UserRole>();

            foreach (var role in roles)
            {
                var ur = await _ctx.UserRoles
                    .Where(x => x.UserId == user.Id && x.RoleId == role.Id)
                    .FirstOrDefaultAsync();

                temp.Add(ur!);
            }

            _ctx.RemoveRange(temp);

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<List<Role>> RolesAsync(User user)
        {
            var roles = await _ctx.UserRoles
                .Where(ops => ops.UserId == user.Id)
                .Include(r => r.Role)
                .Select(item => new Role
                {
                    Id = item.Role.Id,
                    Name = item.Role.Name
                })
                .AsSplitQuery()
                .ToListAsync();

            return roles;
        }

        public async Task<bool> UpdateAsync(EditUserViewModel model)
        {
            var user = await _ctx.Users
                .FindAsync(model.UserId);

            if (user == null) return default;

            if (model.ChangePassword)
            {
                var salt = _hasher.Salt();
                var PasswordHash = _hasher.Hasher(model.Password!, salt);

                user.PasswordHash = PasswordHash;
                user.Salt = salt;
            }

            user.FullName = model.FullName!;
            user.Username = model.Username!;
            user.AccessPrivilege = model.AccessPrivilege!;

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<bool> UpdateUserRoleAsync(User user, Guid Id)
        {
            var role = await _ctx.Roles
                .Where(ops => ops.Id.Equals(Id))
                .FirstOrDefaultAsync();

            var userRole = await _ctx.UserRoles
                .Where(ops => ops.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (userRole != null)
            {
                userRole!.RoleId = role!.Id;
            }
            else
            {
                var tempUserRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role!.Id
                };

                await _ctx.UserRoles.AddRangeAsync(tempUserRole);
            }

            int rowsAffected = await _ctx.SaveChangesAsync();
            return rowsAffected > 0 ? true : default;
        }

        public async Task<User> ValidationAsync(string username, string password)
        {
            var user = await _ctx.Users
                .FirstOrDefaultAsync(x => x.Username == username);

            if (user == null)
                return new User();

            if (user.LockoutEnabled)
            {
                return new User
                {
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount
                };
            }

            if (_hasher.Hasher(password, user.Salt) != user.PasswordHash)
            {
                if (user.AccessFailedCount == 0)
                    user.LockoutEnabled = true;
                else
                    user.AccessFailedCount--;

                await _ctx.SaveChangesAsync();

                return new User
                {
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount
                };
            }

            if (user.AccessFailedCount < 3)
            {
                user.AccessFailedCount = 3;
                await _ctx.SaveChangesAsync();
            }

            return new User
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email!,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                AccessPrivilege = user.AccessPrivilege,
            };
        }
    }
}
