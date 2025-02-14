using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Iachistory
{
    public int IachId { get; set; }

    public DateTime? IachDate { get; set; }

    public int WalletId { get; set; }

    public int? TransactionType { get; set; }

    public string? ProductCode { get; set; }

    public decimal? InvoiceValue { get; set; }

    public decimal? BeginBalance { get; set; }

    public decimal? EndBalance { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
