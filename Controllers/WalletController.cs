using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using BookMoth_Api_With_C_.ZaloPay;
using BookMoth_Api_With_C_.ZaloPay.Crypto;
using BookMoth_Api_With_C_.ZaloPay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using static BookMoth_Api_With_C_.Models.Enums;
using PaymentMethod = BookMoth_Api_With_C_.Models.Enums.PaymentMethod;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly string _appId;
        private readonly string _key1;
        private readonly string _createOrderUrl;
        private BookMothContext _context;
        private ZaloPayHelper _zaloPayHelper;
        private EmailService _emailService;
        private FcmService _fcmService;
        private IConfiguration _config;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            BookMothContext context,
            EmailService emailService,
            FcmService fcmService,
            ZaloPayHelper zaloPayHelper,
            IMemoryCache cache,
            JwtService jwtService,
            ILogger<WalletController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _fcmService = fcmService;
            _logger = logger;
            _zaloPayHelper = zaloPayHelper;
            _config = configuration;

            _appId = _config["ZaloPay:Appid"];
            _key1 = _config["ZaloPay:Key1"];
            _createOrderUrl = _config["ZaloPay:ZaloPayApiCreateOrder"];

        }

        [HttpGet("balance")]
        public IActionResult Get()
        {
            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthorized", error_code = "INVALID_TOKEN" });
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
                return Unauthorized(new { message = "Unauthozired", error_code = "INVALID_TOKEN" });
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
                        Hashedpin = SecurityService.HashPasswordWithSalt(request.Pin, salt),
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
                return Unauthorized(new { message = "Unauthozired", error_code = "INVALID_TOKEN" });
            }

            var accountId = int.Parse(accId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(wallet => wallet.AccountId == accountId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet does not exist" });
            }

            if (wallet.Hashedpin.Equals(SecurityService.HashPasswordWithSalt(confirmPinRequest.Pin, wallet.Salt)))
            {
                return Ok();
            }

            return Unauthorized(new { message = "Invalid PIN", error_code = "INVALID_PIN" });
        }

        [HttpGet("exist")]
        public async Task<IActionResult> isExist()
        {
            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthozired", error_code = "INVALID_TOKEN" });
            }

            var accountId = int.Parse(accId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(wallet => wallet.AccountId == accountId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet does not exist" });
            }

            return Ok();
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.Amount == null)
            {
                return BadRequest(new { message = "Amount is required" });
            }

            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new { message = "Unauthorized", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accId, out var accountId))
            {
                return BadRequest();
            }
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.AccountId == accountId);
            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            var desc = (request.Description == null || request.Description == "") ?
                profile.LastName + " " + profile.FirstName + " NẠP TIỀN" : request.Description;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var countTrans = _context.Transactions.
                Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
                .Count() + 1;


            var items = "[]";

            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );
            Console.WriteLine(vietnamTime);

            var transid = vietnamTime.ToString("yyMMdd_HHmmss") + "_BK" + countTrans.ToString().PadLeft(5, '0');
            var param = new Dictionary<string, string>
            {
                { "appid", _appId },
                { "appuser", accId },
                { "apptime", Util.GetTimeStamp().ToString() },
                { "amount", request.Amount.ToString() },
                { "apptransid", transid},
                { "embeddata", JsonConvert.SerializeObject(new { redirecturl = NgrokHelper.CreateEmbeddataWithPublicUrl()}) },
                { "item", JsonConvert.SerializeObject(items) },
                { "description", desc },
                { "bankcode", "zalopayapp" }
            };

            var data = "553" + "|" + param["apptransid"] + "|" + param["appuser"] + "|" + param["amount"] + "|"
                + param["apptime"] + "|" + param["embeddata"] + "|" + param["item"];
            param.Add("mac", HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, _key1, data));

            var order = await HttpHelper.PostFormAsync(_createOrderUrl, param);

            var returncode = (long)order["returncode"];
            if (returncode == 1)
            {
                using (var trans = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var wallet = _context.Wallets.SingleOrDefault(w => w.AccountId.Equals(accountId));
                        if (wallet == null)
                        {
                            return NotFound(new { success = false, message = "Wallet not found" });
                        }

                        var transaction = new Transaction
                        {
                            TransactionId = transid,
                            ReceiverWalletId = wallet.WalletId,
                            Amount = Decimal.Parse(request.Amount.ToString()),
                            TransactionType = request.TransactionType,
                            Status = 0,
                            CreatedAt = vietnamTime,
                            Description = desc,
                            PaymentMethodId = PaymentMethod.ZaloPay
                        };
                        _context.Transactions.Add(transaction);
                        _context.SaveChanges();
                        trans.Commit();
                        return Ok(new
                        {
                            zaloToken = order["zptranstoken"],
                            transId = transid
                        });

                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        return UnprocessableEntity(new { success = false, message = e.Message });
                    }
                }
            }
            return UnprocessableEntity(new { success = false, message = order["returnmessage"] });
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] CallbackRequest request)
        {
            var result = new Dictionary<string, object>();

            try
            {
                var dataStr = Convert.ToString(request.Data);
                var requestMac = Convert.ToString(request.Mac);

                var isValidCallback = _zaloPayHelper.VerifyCallback(dataStr, requestMac);

                // kiểm tra callback hợp lệ (đến từ zalopay server)
                if (!isValidCallback)
                {
                    // callback không hợp lệ
                    return BadRequest(new { success = false, message = "mac not equal" });
                }
                else
                {
                    // thanh toán thành công
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    var apptransid = data["apptransid"].ToString();

                    var transId = int.Parse(apptransid.Split("_")[2]);

                    using (var trans = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            var transaction = _context.Transactions.SingleOrDefault(o => o.TransactionId.Equals(transId));
                            if (transaction != null)
                            {
                                transaction.Status = 1;
                                _context.SaveChanges();

                                var wallet = _context.Wallets.SingleOrDefault(w => w.WalletId.Equals(transaction.ReceiverWalletId));
                                if (wallet != null)
                                {
                                    wallet.Balance += transaction.Amount;
                                    _context.SaveChanges();
                                    trans.Commit();

                                    //await _notificationService.SendNotification("Nạp tiền thành công!");
                                    return Ok("Success");
                                }
                                else
                                {
                                    trans.Rollback();
                                    return UnprocessableEntity(new { success = false, message = "Wallet not found" });
                                }
                            }
                            else
                            {
                                return UnprocessableEntity(new { success = false, message = "Transaction not found" });
                            }
                        }

                        catch (Exception ex)
                        {
                            trans.Rollback();
                            return UnprocessableEntity(new { success = false, message = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(new { success = false, message = ex.Message });
            }
        }


    }
}
