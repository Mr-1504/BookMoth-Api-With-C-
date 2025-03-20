namespace BookMoth_Api_With_C_.Models
{
    public class Transactions
    {
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public bool TransactionType { get; set; }
        public int Status { get; set; }
        public DateTime Created_At { get; set; }
    }
}
