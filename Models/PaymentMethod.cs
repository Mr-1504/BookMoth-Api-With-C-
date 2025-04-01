using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class PaymentMethod
{
    public Enums.PaymentMethod MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<Iachistory> Iachistories { get; set; } = new List<Iachistory>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
