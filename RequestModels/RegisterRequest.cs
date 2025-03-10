namespace BookMoth_Api_With_C_.RequestModels
{
    public class RegisterRequest
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string Username { get; set; } = null!;

        public string? Avatar = "../Resources/Images/DefaultAvatar.jpg";

        public string? Coverphoto { get; set; }

        public string? Identifier { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int AccountType { get; set; }
    }
}
