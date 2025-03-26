using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using BookMoth_Api_With_C_.ZaloPay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private BookMothContext _context;
        private EmailService _emailService;
        private FcmService _fcmService;
        private IConfiguration _config;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            BookMothContext context,
            EmailService emailService,
            FcmService fcmService,
            ILogger<WalletController> logger)
        {
            _context = context;
            _emailService = emailService;
            _fcmService = fcmService;
            _logger = logger;
        }

        [HttpGet("balance")]
        public IActionResult Get()
        {
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

        [HttpPost("create")]
        public async Task<IActionResult> createWallet([FromBody] CreateWalletRequest request)
        {
            if (request == null)
                return BadRequest();

            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthozired" });
            }

            var accountId = int.Parse(accId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(wallet => wallet.AccountId == accountId);

            if (wallet != null)
            {
                return Conflict(new { message = "Wallet already exists" });
            }
            using (var trans = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var salt = SecurityService.GenerateSalt();

                    var newWallet = new Wallet
                    {
                        AccountId = accountId,
                        Balance = 0,
                        HashedPin = SecurityService.HashPasswordWithSalt(request.Pin, salt),
                        Salt = salt,
                        Status = 1
                    };

                    await _context.Wallets.AddAsync(newWallet);
                    await _context.SaveChangesAsync();

                    await trans.CommitAsync();

                    var deviceTokens = await _context.FcmTokens
                                        .Where(t => t.AccountId == accountId)
                                        .Select(t => t.Token)
                                        .ToListAsync();

                    if (deviceTokens.Any())
                    {
                        var notificationTasks = deviceTokens.Select(async token =>
                        {
                            try
                            {
                                await _fcmService.SendNotificationAsync(
                                    token,
                                    "Mở ví thanh toán thành công",
                                    $"Bạn vừa thực hiện mở ví thanh toán BookMoth\n" +
                                    $"Thời gian: {DateTime.UtcNow.AddHours(7):yyyy-MM-dd HH:mm:ss}\n"
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Lỗi gửi thông báo đến {token}: {ex.Message}");
                            }
                        }).ToList();

                        await Task.WhenAll(notificationTasks);
                    }

                    return Ok();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    return UnprocessableEntity(new { message = ex.Message });
                }
            }

        }

        [HttpPost("confirm-pin")]
        public async Task<IActionResult> confirmPin([FromBody] ConfirmPinRequest confirmPinRequest)
        {
            if (confirmPinRequest == null)
            {
                return BadRequest();
            }

            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthozired" });
            }

            var accountId = int.Parse(accId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(wallet => wallet.AccountId == accountId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet does not exist" });
            }

            if (wallet.HashedPin.Equals(SecurityService.HashPasswordWithSalt(confirmPinRequest.Pin, wallet.Salt)))
            {
                return Ok();
            }

            return Unauthorized(new { message = "Invalid PIN" });
        }

        [HttpGet("exist")]
        public async Task<IActionResult> isExist()
        {
            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthozired" });
            }

            var accountId = int.Parse(accId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(wallet => wallet.AccountId == accountId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet does not exist" });
            }

            return Ok();
        }
    }
}
