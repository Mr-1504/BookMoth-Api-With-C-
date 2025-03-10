using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using BTL_LTWeb.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookMoth_Api_With_C_.Controllers
{
    [ApiController]
    [Route("api/otp")]
    public class OtpController : ControllerBase
    {
        private readonly BookMothContext _context;
        private readonly IMemoryCache _cache;
        private readonly EmailService _emailService;
        private readonly ILogger<OtpController> _logger;

        public OtpController(BookMothContext context, IMemoryCache cache, EmailService emailService, ILogger<OtpController> logger)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Generate and send OTP to email
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RequestOtp([FromBody] GetOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || !IsValidEmail(request.Email))
            {
                return BadRequest(new { message = "Invalid email format." });
            }

            var accountExists = await _context.Accounts.AnyAsync(x => x.Email.ToLower() == request.Email.ToLower());
            
            int a = 0;
            if (accountExists)
            {
                return Conflict(new { message = "Account already exists." });
            }

            var otp = SecurityService.GenerateRandomCode();
            var cacheKey = $"otp:{request.Email.ToLower()}";
            _cache.Set(cacheKey, otp, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(request.Email, request.Name, otp, 1);
                    _logger.LogInformation($"OTP sent to {request.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send OTP to {request.Email}");
                }
            });

            return CreatedAtAction(nameof(VerifyOtp), new { email = request.Email }, new { message = "OTP sent successfully." });
        }

        /// <summary>
        /// Verify OTP for a given email
        /// </summary>
        [HttpPost("verify")]
        public IActionResult VerifyOtp([FromBody] OtpVerificationRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp))
            {
                return BadRequest(new { message = "Email and OTP are required." });
            }

            var cacheKey = $"otp:{request.Email.ToLower()}";
            if (!_cache.TryGetValue(cacheKey, out string cachedOtp) || cachedOtp != request.Otp)
            {
                return Unauthorized(new { message = "Invalid or expired OTP." });
            }

            _cache.Remove(cacheKey);
            return Ok(new { message = "OTP verified successfully." });
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
