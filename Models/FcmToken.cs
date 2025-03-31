using System;
using System.Collections.Generic;

namespace BookMoth_Api_With_C_.Models;

public partial class FcmToken
{
    public int Id { get; set; }

    public int? AccountId { get; set; }

    public string DeviceId { get; set; } = null!;

    public string Token { get; set; } = null!;

    public virtual Account? Account { get; set; }
}
