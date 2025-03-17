using BookMoth_Api_With_C_.Models;
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
        private string url = "http://127.0.0.1:7100/images/";
        private BookMothContext _context;
        private IMemoryCache _cache;
        private EmailService _emailService;
        private JwtService _jwtService;

        public ProfileController(
            BookMothContext context, 
            IMemoryCache cache, 
            EmailService emailService, 
            JwtService jwtService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _jwtService = jwtService;
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

            if (profile == null) {
                return NotFound(new { message = "Profile not found" });
            }

            return Ok(profile);
        }
    }
}
