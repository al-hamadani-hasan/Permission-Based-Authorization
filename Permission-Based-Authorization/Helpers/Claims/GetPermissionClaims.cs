using Permission_Based_Authorization.Claims;
using System.Reflection;

namespace Permission_Based_Authorization.Helpers.Claims
{
    public static class GetPermissionClaims
    {
        public static void GetPermissions(this List<RoleClaims> permissions, Type policy)
        {
            FieldInfo[] fields = policy.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo fi in fields)
            {
                permissions.Add(new RoleClaims { Value = fi.GetValue(null)!.ToString()!, Type = "Permissions" });
            }
        }
    }
}
