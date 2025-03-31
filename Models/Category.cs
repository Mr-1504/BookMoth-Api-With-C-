using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Tag { get; set; } = null!;
}
