using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.ResponseModels;
using BookMoth_Api_With_C_.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Globalization;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private BookMothContext _context;
        private string url = "http://127.0.0.1:7100/images/";
        private IProfileService _profileService;
        private IDistributedCache _cache;
        private IConnectionMultiplexer _redis;

        public ProfileController(
            IConnectionMultiplexer redis,
            IDistributedCache cache,
            IProfileService profileService,
            BookMothContext context)
        {
            _redis = redis;
            _cache = cache;
            _profileService = profileService;
            _context = context;
        }

        /// <summary>
        /// Lấy thông tin cá nhân của người dùng
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var accountId = User.FindFirst("accountId")?.Value;

            if (accountId == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountId, out int accId))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var profile = await _context.Profiles
                .Include(p => p.Follows)
                .FirstOrDefaultAsync(p => p.AccountId == accId);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            int follower = profile.Follows.Count(f => f.FollowingId == profile.ProfileId);
            int following = profile.Follows.Count(f => f.FollowerId == profile.ProfileId);

            var profileResponse = new ProfileResponse
            {
                ProfileId = profile.ProfileId,
                AccountId = profile.AccountId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Username = profile.Username,
                Avatar = profile.Avatar,
                Coverphoto = profile.Coverphoto,
                Gender = profile.Gender,
                Birth = profile.Birth?.ToString("dd/MM/yyyy"),
                Identifier = profile.Identifier,
                Follower = follower,
                Following = following
            };

            return Ok(profileResponse);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> getById(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Follows)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
            {
                return NotFound(new { message = $"Profile {id} not found" });
            }

            int follower = profile.Follows.Count(f => f.FollowingId == profile.ProfileId);
            int following = profile.Follows.Count(f => f.FollowerId == profile.ProfileId);

            var profileResponse = new ProfileResponse
            {
                ProfileId = profile.ProfileId,
                AccountId = profile.AccountId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Username = profile.Username,
                Avatar = profile.Avatar,
                Coverphoto = profile.Coverphoto,
                Gender = profile.Gender,
                Birth = profile.Birth?.ToString("dd/MM/yyyy"),
                Identifier = profile.Identifier,
                Follower = follower,
                Following = following
            };

            return Ok(profileResponse);
        }

        [HttpPost("follow")]
        public async Task<IActionResult> Follow([FromBody] FollowRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var accountIdClaim = User.FindFirst("accountId")?.Value;
            if (accountIdClaim == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var profile = await _context.Profiles
                .Include(p => p.Follows)
                .FirstOrDefaultAsync(p => p.ProfileId == request.Id);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            var myProfile = await _context.Profiles
                .Include(m => m.Follows)
                .FirstOrDefaultAsync(m => m.AccountId == accountId);

            if (myProfile == null)
            {
                return NotFound(new { message = "Your profile not found" });
            }

            if (profile.Follows.Any(f => f.FollowerId == myProfile.ProfileId && f.FollowingId == profile.ProfileId))
            {
                return Conflict(new { message = "Already following this profile" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var follow = new Follow
                {
                    FollowerId = myProfile.ProfileId,
                    FollowingId = profile.ProfileId
                };

                await _context.Follows.AddAsync(follow);
                await _context.SaveChangesAsync();

                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var db = _redis.GetDatabase();
                string pattern = $"search:*profileid:{myProfile.ProfileId}*";
                var keys = server.Keys(pattern: pattern).ToList();
                Console.WriteLine($"Keys found: {string.Join(", ", keys)}"); // Log để kiểm tra
                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                    Console.WriteLine($"Deleted key: {key}");
                }

                string cacheKey = $"follow:{myProfile.ProfileId}";
                string jsonData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(jsonData))
                {
                    var follows = JsonConvert.DeserializeObject<List<int>>(jsonData);

                    follows.Add(profile.ProfileId);
                    string updatedData = JsonConvert.SerializeObject(follows);
                    await _cache.SetStringAsync(cacheKey, updatedData, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
                    });
                }

                await transaction.CommitAsync();

                return Ok(new { message = "Followed successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


        [HttpDelete("follow/{followingId}")]
        public async Task<IActionResult> UnFollow(string followingId)
        {
            if (followingId == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            if (!int.TryParse(followingId, out int id))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var accountIdClaim = User.FindFirst("accountId")?.Value;
            if (accountIdClaim == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var profile = await _context.Profiles
                .Include(p => p.Follows)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            var myProfile = await _context.Profiles
                .Include(m => m.Follows)
                .FirstOrDefaultAsync(m => m.AccountId == accountId);

            if (myProfile == null)
            {
                return NotFound(new { message = "Your profile not found" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var follow = await _context.Follows.FirstOrDefaultAsync(
                    f => f.FollowerId == myProfile.ProfileId && f.FollowingId == profile.ProfileId);

                if (follow != null)
                {
                    _context.Follows.Remove(follow);
                    await _context.SaveChangesAsync();

                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var db = _redis.GetDatabase();
                    string pattern = $"search:*profileid:{myProfile.ProfileId}*";
                    var keys = server.Keys(pattern: pattern).ToList();
                    Console.WriteLine($"Keys found: {string.Join(", ", keys)}"); // Log để kiểm tra
                    foreach (var key in keys)
                    {
                        await db.KeyDeleteAsync(key);
                        Console.WriteLine($"Deleted key: {key}");
                    }

                    string cacheKey = $"follow:{myProfile.ProfileId}";
                    string jsonData = await _cache.GetStringAsync(cacheKey);

                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var follows = JsonConvert.DeserializeObject<List<int>>(jsonData);

                        if (follows.Remove(id))
                        {
                            string updatedData = JsonConvert.SerializeObject(follows);
                            await _cache.SetStringAsync(cacheKey, updatedData, new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) // Giữ nguyên thời gian hết hạn
                            });
                        }
                    }

                    await transaction.CommitAsync();
                    return Ok(new { message = "Followed successfully" });
                }
                else
                {
                    return NotFound(new { message = "not following this profile" });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


        [HttpGet("is-following/{followingId}")]
        public async Task<IActionResult> IsFollowing(int followingId)
        {
            if (followingId == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var accountIdClaim = User.FindFirst("accountId")?.Value;

            if (accountIdClaim == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var myProfile = await _context.Profiles
                .Include(m => m.Follows)
                .FirstOrDefaultAsync(m => m.AccountId == accountId);

            if (myProfile == null)
            {
                return NotFound(new { message = "Your profile not found" });
            }

            string cacheKey = $"follow:{myProfile.ProfileId}";

            // Lấy dữ liệu từ Redis
            var jsonData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(jsonData))
            {
                // Nếu không có cache, lấy từ database
                var follows = await _context.Follows
                    .Where(f => f.FollowerId == myProfile.ProfileId)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                // Cập nhật cache
                jsonData = JsonConvert.SerializeObject(follows);
                await _cache.SetStringAsync(cacheKey, jsonData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
                });
            }

            // Chuyển JSON thành danh sách
            var followList = JsonConvert.DeserializeObject<List<int>>(jsonData);

            // Kiểm tra xem followingId có trong danh sách không
            bool isFollowing = followList.Contains(followingId);

            return Ok(new { isFollowing });
        }



        /// <summary>
        /// Kiểm tra username có tồn tại không
        /// </summary>
        /// <param name="username">Tên người dùng</param>
        /// <returns>Trạng thái username</returns>
        [HttpGet("exists/{username}")]
        public IActionResult CheckUsernameExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username không được để trống!" });
            }

            bool exists = _context.Profiles.Any(u => u.Username == username);

            if (exists)
            {
                return Ok(new { exists = true, message = "Username đã tồn tại!" });
            }

            return Ok(new { exists = false, message = "Username hợp lệ!" });
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="username"></param>
        /// <param name="avatar"></param>
        /// <param name="cover"></param>
        /// <param name="gender"></param>
        /// <param name="identifier"></param>
        /// <param name="birth"></param>
        /// <returns></returns>
        [HttpPatch("edit")]
        public async Task<IActionResult> EditProfile([FromForm] EditProflieRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "invalid request" });
            }

            var accountIdClaim = User.FindFirst("accountId")?.Value;
            if (accountIdClaim == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID" });
            }

            using (var trans = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.AccountId == accountId);

                    if (profile == null)
                    {
                        return NotFound(new { message = "Profile not found" });
                    }

                    if (request.Avatar != null && request.Avatar.Length > 0)
                    {
                        var avatarPath = Path.Combine("Resources/Images/avatars", profile.ProfileId.ToString() + ".png");
                        using (var stream = new FileStream(avatarPath, FileMode.Create))
                        {
                            await request.Avatar.CopyToAsync(stream);
                            profile.Avatar = $"{url}avatars/{profile.ProfileId}.png";
                        }
                    }

                    // Xử lý cover nếu có
                    if (request.Cover != null && request.Cover.Length > 0)
                    {
                        var coverPath = Path.Combine("Resources/Images/covers", profile.ProfileId.ToString() + ".png");
                        using (var stream = new FileStream(coverPath, FileMode.Create))
                        {
                            await request.Cover.CopyToAsync(stream);
                            profile.Coverphoto = $"{url}covers/{profile.ProfileId}.png";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(request.FirstName))
                    {
                        profile.FirstName = request.FirstName;
                    }

                    if (!string.IsNullOrWhiteSpace(request.LastName))
                    {
                        profile.LastName = request.LastName;
                    }

                    if (!string.IsNullOrEmpty(request.Username))
                    {
                        profile.Username = request.Username;
                    }

                    if (!string.IsNullOrWhiteSpace(request.Birth))
                    {
                        profile.Birth = DateTime.ParseExact(request.Birth, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }

                    if (profile.Gender.HasValue && profile.Gender >= 0)
                    {
                        profile.Gender = profile.Gender;
                    }

                    if (request.Identifier.HasValue)
                    {
                        profile.Identifier = request.Identifier;
                    }

                    await _context.SaveChangesAsync();
                    await trans.CommitAsync();

                    return Ok(profile);
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return UnprocessableEntity(new { message = ex.Message });
                }
            }
        }

        /// <summary>
        /// Tìm kiếm người dùng theo mutual follow
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return BadRequest(new { message = "Invalid search string" });
            }

            var accountIdClaim = User.FindFirst("accountId")?.Value;
            if (accountIdClaim == null)
            {
                return Unauthorized(new { message = "User not authenticated", error_code = "INVALID_TOKEN" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID", error_code = "INVALID_TOKEN" });
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.AccountId == accountId);
            if (profile == null)
            {
                return NotFound(new { message = "Profile not found", error_code = "INVALID_PROFILE" });
            }

            string cacheKey = $"search:{search}profileid:{profile.ProfileId}";
            string cachedResult = null;
                //await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var _profiles = JsonConvert.DeserializeObject<List<ProfileDTO>>(cachedResult);
                Console.WriteLine("Get from cache");
                return Ok(_profiles);
            }

            var profiles = await _profileService.SearchUsersByFollowAsync(profile.ProfileId, search);

            if (profiles == null || profiles.Count == 0)
            {
                return NotFound(new { message = "Không tìm thấy người dùng nào." });
            }

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(profiles), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return Ok(profiles);
        }

    }
}
