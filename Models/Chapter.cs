using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public int WorkId { get; set; }

    public string? Title { get; set; }

    public DateTime PostDate { get; set; }

    public string ContentUrl { get; set; } = null!;

    public virtual Work Work { get; set; } = null!;
}
