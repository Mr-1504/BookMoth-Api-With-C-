using BookMoth_Api_With_C_.Models;
using Newtonsoft.Json.Linq;
using static BookMoth_Api_With_C_.Models.Enums;

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

                    DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                    foreach (var transaction in pendingTransactions)
                    {
                        try
                        {
                            var response = await zaloPayService.GetTransactionStatusAsync(transaction.TransactionId);

                            if (string.IsNullOrWhiteSpace(response))
                            {
                                _logger.LogError($"Phản hồi từ ZaloPay rỗng hoặc null cho giao dịch {transaction.TransactionId}");
                                continue;
                            }

                            JObject jsonResponse;
                            try
                            {
                                jsonResponse = JObject.Parse(response);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Lỗi khi parse JSON từ ZaloPay cho giao dịch {transaction.TransactionId}: {ex.Message}");
                                continue;
                            }

                            if (jsonResponse["returncode"] != null && (int)jsonResponse["returncode"] == 1)
                            {
                                transaction.Status = 1;
                                var wallet = context.Wallets.SingleOrDefault(w => w.WalletId == transaction.ReceiverWalletId);

                                if (wallet != null)
                                {
                                    decimal oldBalance = wallet.Balance;
                                    wallet.Balance += transaction.Amount;

                                    await context.SaveChangesAsync();
                                    var history = new Iachistory
                                    {
                                        IachDate = vietnamTime,
                                        ReceiverWalletId = transaction.ReceiverWalletId,
                                        TransactionType = TransactionType.Deposit,
                                        BeginBalance = oldBalance,
                                        EndBalance = wallet.Balance,
                                        Description = transaction.Description,
                                        PaymentMethodId = transaction.PaymentMethodId
                                    };

                                    await context.Iachistories.AddAsync(history);

                                    await context.SaveChangesAsync();

                                    var deviceTokens = context.FcmTokens
                                        .Where(t => t.AccountId == wallet.AccountId)
                                        .Select(t => t.Token)
                                        .ToList();

                                    if (deviceTokens.Any())
                                    {
                                        if (jsonResponse["apptime"] != null && long.TryParse(jsonResponse["apptime"].ToString(), out long timestamp))
                                        {
                                            DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime.AddHours(7);

                                            var notificationTasks = deviceTokens.Select(async token =>
                                            {
                                                try
                                                {
                                                    await _fcmService.SendNotificationAsync(
                                                        token,
                                                        "Thanh toán thành công",
                                                        $"Bạn vừa thực hiện thành công một giao dịch\n" +
                                                        $"Thời gian: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                                        $"Giao dịch: +{transaction.Amount:N0} VND\n" +
                                                        $"Số dư hiện tại: {wallet.Balance:N0} VND\n" +
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
                                        else
                                        {
                                            _logger.LogError($"Dữ liệu apptime không hợp lệ cho giao dịch {transaction.TransactionId}");
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogError($"Không tìm thấy ví có ID {transaction.ReceiverWalletId} cho giao dịch {transaction.TransactionId}");
                                }
                            }
                            else if (transaction.Status == 0 && (vietnamTime - transaction.CreatedAt).TotalMinutes > 15)
                            {
                                transaction.Status = -1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Lỗi kiểm tra giao dịch {transaction.TransactionId}: {ex.Message}");
                        }
                    }
                }
                await Task.Delay(delayTime, stoppingToken);
            }
        }


    }
}
