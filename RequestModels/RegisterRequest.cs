namespace BookMoth_Api_With_C_.RequestModels
{
    public class RegisterRequest
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Username { get; set; }

        public string? Avatar = "../Resources/Images/avatar.jpg";

        public string? Coverphoto { get; set; }

        public bool? Identifier { get; set; }
        public int? Gender { get; set; }
        public string DateOfBirth { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int AccountType { get; set; }
    }
}
