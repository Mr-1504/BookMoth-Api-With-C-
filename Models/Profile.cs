using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class Profile
{
    public int ProfileId { get; set; }

    public int AccountId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string Username { get; set; } = null!;

    public string? Avatar { get; set; }

    public string? Coverphoto { get; set; }

    public bool? Identifier { get; set; }

    public int? Gender { get; set; }

    public DateTime? Birth { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Follow> Follows { get; set; } = new List<Follow>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Work> Works { get; set; } = new List<Work>();
}
