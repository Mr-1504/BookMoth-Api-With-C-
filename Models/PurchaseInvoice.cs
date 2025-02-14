using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class PurchaseInvoice
{
    public int PurchaseId { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public int WalletId { get; set; }

    public int? Quantity { get; set; }

    public decimal? BeginBalance { get; set; }

    public decimal? EndBalance { get; set; }

    public string? BankInvoiceCode { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
