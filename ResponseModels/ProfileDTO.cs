namespace BookMoth_Api_With_C_.ResponseModels
{
    public class ProfileDTO
    {
        public int Profile_Id { get; set; }
        public string Username { get; set; } = null!;
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }
        public string? Avatar { get; set; }
        public int MutualCount { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public int Followed { get; set; }
    }
}
