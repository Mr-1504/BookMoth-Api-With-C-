namespace BookMoth_Api_With_C_.Models
{
    public class Enums
    {
        public enum TransactionType
        {
            Deposit = 1,
            Withdrawal = 2,
            Transfer = 3,
            Payment = 4,
        }

        public enum PaymentMethod
        {
            ZaloPay = 1,
            Wallet = 2
        }

    }
}
