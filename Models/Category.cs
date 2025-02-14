using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string? Category1 { get; set; }

    public string? Tag { get; set; }

    public virtual ICollection<Work> Works { get; set; } = new List<Work>();
}
