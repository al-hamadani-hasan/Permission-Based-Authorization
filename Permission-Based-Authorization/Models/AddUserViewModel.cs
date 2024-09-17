namespace Permission_Based_Authorization.Models
{
    public class AddUserViewModel
    {
        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? AccessPrivilege { get; set; }
    }
}
