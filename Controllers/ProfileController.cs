using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.ResponseModels;
using BookMoth_Api_With_C_.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private BookMothContext _context;

        public ProfileController(
            BookMothContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var accountId = User.FindFirst("accountId")?.Value;

            if (accountId == null)
            {
                return Unauthorized(new { message = "Unauthorized" });
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
    }
}
