namespace Permission_Based_Authorization.Models
{
    public class EditUserViewModel : AddUserViewModel
    {
        public Guid UserId { get; set; }
        public bool ChangePassword { get; set; } = false;
    }
}
