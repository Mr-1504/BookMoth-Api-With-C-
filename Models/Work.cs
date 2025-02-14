using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Work
{
    public int WorkId { get; set; }

    public int ProfileId { get; set; }

    public int ChapterId { get; set; }

    public int CategoryId { get; set; }

    public DateTime? PostDate { get; set; }

    public decimal? Price { get; set; }

    public int? ViewCount { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual Profile Profile { get; set; } = null!;
}
