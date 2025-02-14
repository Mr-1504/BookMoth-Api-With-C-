using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class PaymentInvoice
{
    public int PaymentId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public int AuthorWalletId { get; set; }

    public decimal? Amount { get; set; }

    public decimal? BeginBalance { get; set; }

    public decimal? EndBalance { get; set; }

    public string? BankInvoiceCode { get; set; }

    public virtual AuthorWallet AuthorWallet { get; set; } = null!;
}
