namespace Permission_Based_Authorization.Claims
{
    public class PermissionClaims
    {
        public string RoleId { get; set; } = string.Empty;
        public IList<RoleClaims> RoleClaims { get; set; } = new List<RoleClaims>();
    }
}
