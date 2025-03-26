using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using BookMoth_Api_With_C_.ZaloPay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using BookMoth_Api_With_C_.ZaloPay;
using Newtonsoft.Json;
using BookMoth_Api_With_C_.ZaloPay.Crypto;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly string _appId;
        private readonly string _key1;
        private readonly string _createOrderUrl;
        private BookMothContext _context;
        private ZaloPayHelper _zaloPayHelper;
        private IMemoryCache _cache;
        private EmailService _emailService;
        private JwtService _jwtService;
        private IConfiguration _config;

        public TransactionController(
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


            _appId = _config["ZaloPay:Appid"];
            _key1 = _config["ZaloPay:Key1"];
            _createOrderUrl = _config["ZaloPay:ZaloPayApiCreateOrder"];
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
                return Unauthorized(new { message = "Unauthorized" });
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
                Where(t => t.Created_At >= today && t.Created_At < tomorrow)
                .Count() + 1;


            var items = "[]";

            DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );
            Console.WriteLine(vietnamTime);

            var transid = vietnamTime.ToString("yyMMdd_HHmmss") + "_BK" + countTrans.ToString().PadLeft(5, '0');
            var param = new Dictionary<string, string>
            {
                { "appid", "553" },
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

                        var transaction = new Transactions
                        {
                            TransactionId = transid,
                            WalletId = wallet.WalletId,
                            Amount = Decimal.Parse(request.Amount.ToString()),
                            TransactionType = request.TransactionType,
                            Status = 0,
                            Created_At = vietnamTime,
                            Description = desc
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

                                var wallet = _context.Wallets.SingleOrDefault(w => w.WalletId.Equals(transaction.WalletId));
                                if (wallet != null)
                                {
                                    wallet.Balance += transaction.Amount;
                                    _context.SaveChanges();
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

        //[HttpGet("history")]
        //public async Task<IActionResult> getHistory()
        //{
        //    var accId = User.FindFirst("accountId")?.Value;
        //    if (accId == null)
        //    {
        //        return Unauthorized(new { message = "Unauthorized" });
        //    }

        //    if (string.IsNullOrEmpty(accId) || !int.TryParse(accId, out var accountId))
        //    {
        //        return BadRequest(new { message = "Invalid accountId" });
        //    }

        //    if (transactions.Any())
        //    {
        //        return NoContent();
        //    }

        //    return Ok(transactions);
        //}
    }
}
