using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.Services;
using BookMoth_Api_With_C_.ZaloPay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private BookMothContext _context;
        private ZaloPayHelper _zaloPayHelper;
        private IMemoryCache _cache;
        private EmailService _emailService;
        private JwtService _jwtService;
        private IConfiguration _config;

        public WalletController(
            BookMothContext context,
            ZaloPayHelper zaloPayHelper,
            IMemoryCache cache,
            EmailService emailService,
            JwtService jwtService,
            IConfiguration configuration)
        {
            _context = context;
            _zaloPayHelper = zaloPayHelper;
            _cache = cache;
            _emailService = emailService;
            _jwtService = jwtService;
            _config = configuration;
        }

        [HttpGet("balance")]
        public IActionResult Get() {
            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var accountId = int.Parse(accId);

            var wallet = _context.Wallets.SingleOrDefault(w => w.AccountId == accountId);
            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            return Ok(new { balance = wallet.Balance });
        }
    }
}
