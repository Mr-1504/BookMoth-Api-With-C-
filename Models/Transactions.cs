using static BookMoth_Api_With_C_.Models.Enums;

namespace BookMoth_Api_With_C_.Models
{
    public class Transactions
    {
        public required string TransactionId { get; set; }
        public int? SenderWalletId { get; set; }
        public int? ReceiverWalletId { get; set; }
        public PaymentMethod Payment_Method_Id { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public int Status { get; set; }
        public required string Description { get; set; }
        public DateTime Created_At { get; set; }
    }
}
