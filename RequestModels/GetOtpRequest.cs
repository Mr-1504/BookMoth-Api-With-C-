namespace BookMoth_Api_With_C_.RequestModels
{
    public class GetOtpRequest
    {
        public string Email { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
