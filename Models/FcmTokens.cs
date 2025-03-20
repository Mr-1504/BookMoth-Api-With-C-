namespace BookMoth_Api_With_C_.Models
{
    public class FcmTokens
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Token { get; set; }
        public string DeviceId { get; set; }
    }
}
