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

    public string? Identifier { get; set; }
    public int? Gender { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Work> Works { get; set; } = new List<Work>();

    public virtual ICollection<Profile> Followers { get; set; } = new List<Profile>();

    public virtual ICollection<Profile> Followings { get; set; } = new List<Profile>();
}
