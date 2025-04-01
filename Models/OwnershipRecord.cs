using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class OwnershipRecord
{
    public int WorkId { get; set; }

    public int AccountId { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Work Work { get; set; } = null!;
}
