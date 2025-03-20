namespace BookMoth_Api_With_C_.RequestModels
{
    public class CreateOrderRequest
    {
        public long Amount { get; set; }
        public bool TransactionType { get; set; }
        public string Description { get; set; }
    }
}
