using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Worktag
{
    public int WorkId { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Work Work { get; set; } = null!;
}
