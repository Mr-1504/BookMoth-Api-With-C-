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
                int delayTime = 10000;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var zaloPayService = scope.ServiceProvider.GetRequiredService<ZaloPayService>();
                    var context = scope.ServiceProvider.GetRequiredService<BookMothContext>();

                    var pendingTransactions = context.Transactions
                        .Where(t => t.Status == 0) // Chỉ kiểm tra giao dịch đang chờ xử lý
                        .ToList();

                    delayTime = pendingTransactions.Any() ? 10000 : 30000;

                    bool hasChanges = false;

                    foreach (var transaction in pendingTransactions)
                    {
                        DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                        try
                        {
                            var apptransid = transaction.Created_At.ToString("yyMMdd") + "_" + transaction.TransactionId.ToString().PadLeft(10, '0');
                            var response = await zaloPayService.GetTransactionStatusAsync(apptransid);
                            var jsonResponse = JObject.Parse(response);

                            if ((int)jsonResponse["returncode"] == 1) // Thành công
                            {
                                transaction.Status = 1;
                                var wallet = context.Wallets.SingleOrDefault(w => w.WalletId == transaction.WalletId);
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
                                    var notificationTasks = deviceTokens.Select(async token =>
                                    {
                                        try
                                        {
                                            await _fcmService.SendNotificationAsync(
                                                token,
                                                "Thanh toán thành công",
                                                $"Bạn đã nạp {transaction.Amount} VNĐ vào ví thành công!"
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
