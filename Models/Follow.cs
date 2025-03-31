using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Follow
{
    public int FollowerId { get; set; }

    public int FollowingId { get; set; }

    public virtual Profile Following { get; set; } = null!;
}
