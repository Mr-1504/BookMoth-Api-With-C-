using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int AccountId { get; set; }

    public decimal Balance { get; set; }

    public string Hashedpin { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public int Status { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Iachistory> IachistoryReceiverWallets { get; set; } = new List<Iachistory>();

    public virtual ICollection<Iachistory> IachistorySenderWallets { get; set; } = new List<Iachistory>();
}
