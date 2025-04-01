using BookMoth_Api_With_C_.Models;

namespace BookMoth_Api_With_C_.RequestModels
{
    public class UpdatePaymentMethodRequest
    {
        public string TransactionId { get; set; }
        public Enums.PaymentMethod PaymentMethodId { get; set; }
    }
}
