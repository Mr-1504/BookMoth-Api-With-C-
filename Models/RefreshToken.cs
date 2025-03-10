using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class RefreshToken
{
    public int TokenId { get; set; }

    public int AccountId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    //vô hiệu hóa, thu hồi refresh token
    public DateTime? RevokedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
