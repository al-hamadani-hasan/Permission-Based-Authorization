namespace Permission_Based_Authorization.Models
{
    public class UserRolesViewModel
    {
        public Guid RoleId { get; set; }
        public required string RoleName { get; set; }
        public bool Selected { get; set; }
    }
}
