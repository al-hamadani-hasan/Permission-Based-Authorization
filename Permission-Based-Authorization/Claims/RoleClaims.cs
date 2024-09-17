namespace Permission_Based_Authorization.Claims
{
    public class RoleClaims
    {
        public required string Type { get; set; }
        public required string Value { get; set; }
        public bool Selected { get; set; } = false;
    }
}
