using BookMoth_Api_With_C_.Models;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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
                        .Where(t => t.Status == 0 && t.PaymentMethodId == Enums.PaymentMethod.ZaloPay)
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
                                if (transaction.TransactionId.Contains("PM"))
                                {
                                    var senderWallet = await context.Wallets
                                        .FromSqlRaw("SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE wallet_id = {0}", transaction.SenderWalletId)
                                        .FirstOrDefaultAsync();

                                    if (senderWallet == null)
                                    {
                                        continue;
                                    }

                                    var receiverWallet = await context.Wallets
                                        .FromSqlRaw("SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE wallet_id = {0}", transaction.ReceiverWalletId)
                                        .FirstOrDefaultAsync();

                                    if (receiverWallet == null)
                                    {
                                        continue;
                                    }

                                    senderWallet.Balance -= transaction.Amount;
                                    receiverWallet.Balance += transaction.Amount;
                                    transaction.Status = TransactionStatus.Success;

                                    await context.SaveChangesAsync();

                                    DateTime time = DateTime.UtcNow.AddHours(7);

                                    var history = new Iachistory
                                    {
                                        IachDate = time,
                                        TransactionType = transaction.TransactionType,
                                        InvoiceValue = transaction.Amount,
                                        BeginBalance = senderWallet.Balance,
                                        EndBalance = senderWallet.Balance - transaction.Amount,
                                        SenderWalletId = senderWallet.WalletId,
                                        ReceiverWalletId = receiverWallet.WalletId,
                                        Description = transaction.Description,
                                        PaymentMethodId = transaction.PaymentMethodId,
                                        WorkId = transaction.WorkId,
                                        TransactionId = transaction.TransactionId
                                    };
                                    await context.Iachistories.AddAsync(history);


                                    var receiverHistory = new Iachistory
                                    {
                                        IachDate = time,
                                        TransactionType = transaction.TransactionType,
                                        InvoiceValue = transaction.Amount,
                                        BeginBalance = receiverWallet.Balance,
                                        EndBalance = receiverWallet.Balance + transaction.Amount,
                                        SenderWalletId = senderWallet.WalletId,
                                        ReceiverWalletId = receiverWallet.WalletId,
                                        Description = transaction.Description,
                                        PaymentMethodId = transaction.PaymentMethodId,
                                        WorkId = transaction.WorkId,
                                        TransactionId = transaction.TransactionId
                                    };
                                    await context.Iachistories.AddAsync(receiverHistory);

                                    await context.SaveChangesAsync();

                                    var owner = new OwnershipRecord
                                    {
                                        WorkId = (int)transaction.WorkId,
                                        AccountId = senderWallet.AccountId
                                    };
                                    await context.OwnershipRecords.AddAsync(owner);
                                    await context.SaveChangesAsync();

                                    string message = $"Bạn vừa thực hiện thành công một giao dịch\n" +
                                        $"Thời gian: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"Giao dịch: -{transaction.Amount:N0} VND\n" +
                                        $"Số dư hiện tại: {senderWallet.Balance:N0} VND\n" +
                                        $"Nội dung: {transaction.Description}";

                                    string title = "Thanh toán thành công";
                                    sendNotificationAsync(context, senderWallet, title, message).Wait();

                                    message = $"Bạn vừa nhận được một giao dịch\n" +
                                        $"Thời gian: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"Giao dịch: +{transaction.Amount:N0} VND\n" +
                                        $"Số dư hiện tại: {receiverWallet.Balance:N0} VND\n" +
                                        $"Nội dung: {transaction.Description}";
                                    title = "Biến động số dư";
                                    sendNotificationAsync(context, receiverWallet, title, message).Wait();
                                }
                                else
                                {
                                    transaction.Status = TransactionStatus.Success;
                                    var wallet = context.Wallets
                                        .FromSqlRaw("SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE wallet_id = {0}", transaction.ReceiverWalletId)
                                        .FirstOrDefault();
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
                                            InvoiceValue = transaction.Amount,
                                            BeginBalance = oldBalance,
                                            EndBalance = wallet.Balance,
                                            Description = transaction.Description,
                                            PaymentMethodId = transaction.PaymentMethodId,
                                            WorkId = transaction.WorkId,
                                            TransactionId = transaction.TransactionId
                                        };

                                        await context.Iachistories.AddAsync(history);
                                        await context.SaveChangesAsync();
                                        if (jsonResponse["apptime"] != null && long.TryParse(jsonResponse["apptime"].ToString(), out long timestamp))
                                        {
                                            DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime.AddHours(7);

                                            string message = $"Bạn vừa thực hiện thành công một giao dịch\n" +
                                                $"Thời gian: {time:yyyy-MM-dd HH:mm:ss}\n" +
                                                $"Giao dịch: +{transaction.Amount:N0} VND\n" +
                                                $"Số dư hiện tại: {wallet.Balance:N0} VND\n" +
                                                $"Nội dung: Nạp tiền: {transaction.Description}";
                                            string title = "Nạp tiền thành công";

                                            sendNotificationAsync(context, wallet, title, message).Wait();
                                        }
                                        else
                                        {
                                            _logger.LogError($"Dữ liệu apptime không hợp lệ cho giao dịch {transaction.TransactionId}");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError($"Không tìm thấy ví có ID {transaction.ReceiverWalletId} cho giao dịch {transaction.TransactionId}");
                                    }
                                }

                            }
                            else if (transaction.Status == 0 && (vietnamTime - transaction.CreatedAt).TotalMinutes > 15)
                            {
                                transaction.Status = TransactionStatus.Failed;
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

        public async Task sendNotificationAsync(BookMothContext context, Wallet wallet, string title, string message)
        {
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
                        await _fcmService.SendNotificationAsync(token, title, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi gửi thông báo đến {token}: {ex.Message}");
                    }
                }).ToList();

                await Task.WhenAll(notificationTasks);

            }
        }

    }
}
