using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class RefreshToken
{
    public int TokenId { get; set; }

    public int AccountId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedDate { get; set; }

    public string? RevokedByIp { get; set; }

    public bool IsActive { get; set; }

    public virtual Account Account { get; set; } = null!;
}
