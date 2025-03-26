using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public decimal Balance { get; set; }
    public required string HashedPin { get; set; }
    public required string Salt { get; set; }

    public int Status { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Iachistory> Iachistories { get; set; } = new List<Iachistory>();

    public virtual ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
}
