using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class AuthorWallet
{
    public int WalletId { get; set; }

    public string? Managers { get; set; }

    public decimal? AccumulatedBalance { get; set; }

    public string? PaymentInfo { get; set; }

    public virtual ICollection<PaymentInvoice> PaymentInvoices { get; set; } = new List<PaymentInvoice>();
}
