using BookMoth_Api_With_C_.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace BookMoth_Api_With_C_.Services
{
    public class FollowCacheRefreshService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDistributedCache _cache;

        public FollowCacheRefreshService(IServiceScopeFactory scopeFactory, IDistributedCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<BookMothContext>();
                        var profiles = await dbContext.Profiles.ToListAsync();
                        // Lấy danh sách các User có cache trong Redis
                        foreach (var profile in profiles) // Giả sử có tối đa 100k user
                        {
                            string cacheKey = $"follow:{profile.ProfileId}";
                            string jsonData = await _cache.GetStringAsync(cacheKey);

                            if (!string.IsNullOrEmpty(jsonData))
                            {
                                // Cập nhật cache trước khi nó hết hạn
                                var follows = await dbContext.Follows
                                    .Where(f => f.FollowerId == profile.ProfileId)
                                    .Select(f => f.FollowingId)
                                    .ToListAsync();

                                string updatedData = JsonConvert.SerializeObject(follows);
                                await _cache.SetStringAsync(cacheKey, updatedData, new DistributedCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) // Gia hạn thêm 6h
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Refresh Follow Cache: {ex.Message}");
                }

                // Chạy lại sau 30 phút
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}
