namespace BookMoth_Api_With_C_.ResponseModels
{
    public class ProfileResponse
    {
        public int ProfileId { get; set; }

        public int AccountId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string Username { get; set; } = null!;

        public string? Avatar { get; set; }

        public string? Coverphoto { get; set; }
        public int? Gender { get; set; }

        public string? Birth { get; set; }

        public bool? Identifier { get; set; }

        public int Follower { get; set; }
        public int Following { get; set; }
    }
}
