using static BookMoth_Api_With_C_.Models.Enums;

namespace BookMoth_Api_With_C_.RequestModels
{
    public class CreateOrderRequest
    {
        public long Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Description { get; set; }
    }
}
