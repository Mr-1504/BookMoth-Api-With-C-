namespace BookMoth_Api_With_C_.ResponseModels
{
    public class AccountResponse
    {
        public int AccountId { get; set; }

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public int? AccountType { get; set; }
    }
}
