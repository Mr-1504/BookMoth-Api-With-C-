using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Work
{
    public int WorkId { get; set; }

    public int ProfileId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime PostDate { get; set; }

    public decimal Price { get; set; }

    public long ViewCount { get; set; }

    public string? Description { get; set; }

    public string? CoverUrl { get; set; }

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual Profile Profile { get; set; } = null!;
}
