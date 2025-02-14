using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int? AccountType { get; set; }

    public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
