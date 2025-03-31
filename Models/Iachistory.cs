using static BookMoth_Api_With_C_.Models.Enums;

namespace BookMoth_Api_With_C_.Models;

public partial class Iachistory
{
    public int IachId { get; set; }

    public DateTime IachDate { get; set; }

    public TransactionType TransactionType { get; set; }

    public string? ProductCode { get; set; }
    public string? TransactionId { get; set; }

    public decimal? InvoiceValue { get; set; }

    public decimal BeginBalance { get; set; }

    public decimal EndBalance { get; set; }

    public int? SenderWalletId { get; set; }

    public int ReceiverWalletId { get; set; }
    public int? WorkId { get; set; }

    public string Description { get; set; } = null!;

    public Enums.PaymentMethod PaymentMethodId { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual Wallet ReceiverWallet { get; set; } = null!;

    public virtual Wallet? SenderWallet { get; set; }
}
