using BookMoth_Api_With_C_.Models;
using Newtonsoft.Json.Linq;

namespace BookMoth_Api_With_C_.Services
{
    public class TransactionBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TransactionBackgroundService> _logger;
        private readonly FcmService _fcmService;
        public TransactionBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TransactionBackgroundService> logger,
            FcmService fcmService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _fcmService = fcmService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int delayTime = 5000;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var zaloPayService = scope.ServiceProvider.GetRequiredService<ZaloPayService>();
                    var context = scope.ServiceProvider.GetRequiredService<BookMothContext>();

                    var pendingTransactions = context.Transactions
                        .Where(t => t.Status == 0) 
                        .ToList();

                    delayTime = pendingTransactions.Any() ? 5000 : 20000;

                    bool hasChanges = false;

                    foreach (var transaction in pendingTransactions)
                    {
                        DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                        try
                        {
                            var response = await zaloPayService.GetTransactionStatusAsync(transaction.TransactionId);
                            var jsonResponse = JObject.Parse(response);

                            if ((int)jsonResponse["returncode"] == 1)
                            {
                                transaction.Status = 1;
                                var wallet = context.Wallets.SingleOrDefault(w => w.WalletId == transaction.ReceiverWalletId);
                                if (wallet != null)
                                {
                                    wallet.Balance += transaction.Amount;
                                }

                                hasChanges = true;

                                var deviceTokens = context.FcmTokens
                                    .Where(t => t.AccountId == wallet.AccountId)
                                    .Select(t => t.Token)
                                    .ToList();

                                if (deviceTokens.Any())
                                {
                                    long timestamp = long.Parse(jsonResponse["apptime"].ToString());
                                    DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime.AddHours(7);



                                    var notificationTasks = deviceTokens.Select(async token =>
                                    {
                                        try
                                        {
                                            await _fcmService.SendNotificationAsync(
                                                token,
                                                "Thanh toán thành công",
                                                $"Bạn vừa thực hiện thành công một giao dịch\n" +
                                                $"Thời gian: {time.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                                                $"Giao dịch: +{transaction.Amount.ToString("N0")} VND\n" +
                                                $"Số dư hiện tại: {wallet.Balance.ToString("N0")} VND\n" +
                                                $"Nội dung: Nạp tiền: {transaction.Description}"
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError($"Lỗi gửi thông báo đến {token}: {ex.Message}");
                                        }
                                    }).ToList();

                                    await Task.WhenAll(notificationTasks);
                                }
                            }
                            else if (transaction.Status == 0 && (vietnamTime - transaction.Created_At).TotalMinutes > 15)
                            {
                                transaction.Status = -1;
                                hasChanges = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi kiểm tra giao dịch {transaction.TransactionId}: {ex.Message}");
                        }
                    }

                    if (hasChanges)
                    {
                        await context.SaveChangesAsync();
                    }
                }
                await Task.Delay(5000, stoppingToken);
            }
        }

    }
}
