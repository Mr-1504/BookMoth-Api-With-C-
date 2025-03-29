using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.ResponseModels;
using BookMoth_Api_With_C_.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private BookMothContext _context;
        private string url = "http://127.0.0.1:7100/images/";

        public ProfileController(
            BookMothContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var accountId = User.FindFirst("accountId")?.Value;

            if (accountId == null)
            {
                return Unauthorized(new { message = "Unauthorized", error_code = "INVALID_TOKEN" });
            }


            var accId = Convert.ToInt32(accountId);

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.AccountId == accId);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            int follower = profile.Followers.Count();
            int following = profile.Followings.Count();

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
                .Include(p => p.Followings)
                .Include(p => p.Followers)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
            {
                return NotFound(new { message = $"Profile {id} not found" });
            }

            int follower = profile.Followers.Count();
            int following = profile.Followings.Count();

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
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Invalid account ID" });
            }

            var profile = await _context.Profiles
                .Include(p => p.Followings)
                .Include(p => p.Followers)
                .FirstOrDefaultAsync(p => p.ProfileId == request.Id);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            var myProfile = await _context.Profiles
                .Include(m => m.Followings)
                .Include(m => m.Followers)
                .FirstOrDefaultAsync(m => m.AccountId == accountId);

            if (myProfile == null)
            {
                return NotFound(new { message = "Your profile not found" });
            }

            if (profile.Followers.Contains(myProfile))
            {
                return Conflict(new { message = "Already following this profile" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                profile.Followers.Add(myProfile);
                myProfile.Followings.Add(profile);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Followed successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
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
    }
}
