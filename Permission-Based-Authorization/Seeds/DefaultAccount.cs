using Permission_Based_Authorization.Contexts.UserManagement;
using Permission_Based_Authorization.Contexts.UserManagement.Models;
using Permission_Based_Authorization.Services;

namespace Permission_Based_Authorization.Seeds
{
    public class DefaultAccount
    {
        public static void Seed(
            UserManagementDbContext ctx,
            IHasherService hasher)
        {
            if (!ctx.Users.Any())
            {
                var Salt = hasher
                    .Salt();
                var PasswordHash = hasher
                    .Hasher("Administrator", Salt);

                var roles = ctx
                    .Roles
                    .ToList();

                var RoleId = roles
                    .Where(o => o.Name == "Administrator")
                    .FirstOrDefault()!
                    .Id;

                Guid newUserId = Guid.NewGuid();

                var user = new User
                {
                    Id = newUserId,
                    FullName = "Administrator",
                    Username = "Administrator",
                    NormalizedUsername = "Administrator".ToUpper().Normalize(),
                    Email = "your-email@email.com",
                    NormalizedEmail = "your-email@email.com".ToUpper().Normalize(),
                    PasswordHash = PasswordHash,
                    Salt = Salt,
                    AccessFailedCount = 3,
                    AccessPrivilege = "{}"
                };

                var createdUser = ctx
                    .Users
                    .Add(user);

                ctx.SaveChanges();

                var userRole = new UserRole
                {
                    UserId = createdUser.Entity.Id,
                    RoleId = RoleId
                };

                ctx.UserRoles
                    .Add(userRole);
                ctx.SaveChanges();
            }
        }
    }
}
