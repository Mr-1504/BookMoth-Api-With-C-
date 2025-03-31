namespace BookMoth_Api_With_C_.Models;

public partial class Transaction
{
    public string TransactionId { get; set; } = null!;

    public decimal Amount { get; set; }

    public Enums.TransactionType TransactionType { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Description { get; set; } = null!;

    public Enums.PaymentMethod PaymentMethodId { get; set; }

    public int? SenderWalletId { get; set; }

    public int ReceiverWalletId { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
}
