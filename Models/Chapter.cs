using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public DateTime? PostDate { get; set; }

    public string? FileUrl { get; set; }

    public virtual ICollection<Work> Works { get; set; } = new List<Work>();
}
