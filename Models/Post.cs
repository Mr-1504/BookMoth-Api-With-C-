using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Post
{
    public int PostId { get; set; }

    public int AuthorId { get; set; }

    public virtual Profile Author { get; set; } = null!;
}
