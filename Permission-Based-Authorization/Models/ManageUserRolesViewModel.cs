namespace Permission_Based_Authorization.Models
{
    public class ManageUserRolesViewModel
    {
        public required Guid UserId { get; set; }
        public required IList<UserRolesViewModel> UserRoles { get; set; }
    }
}
