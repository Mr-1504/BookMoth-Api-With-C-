using Azure.Core;
using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using BookMoth_Api_With_C_.ViewModels;
using BTL_LTWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LoginRequest = BookMoth_Api_With_C_.ViewModels.LoginRequest;
using RegisterRequest = BookMoth_Api_With_C_.RequestModels.RegisterRequest;



namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private BookMothContext _context;
        private IMemoryCache _cache;
        private EmailService _emailService;

        public AccountController(BookMothContext context, IMemoryCache cache, EmailService emailService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
        }


        // GET /<AccountController>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            Account account = _context.Accounts.FirstOrDefault(x => x.AccountId == id);
            if (account == null)
            {
                return NotFound();
            }
            AccountViewModel accountViewModel = new AccountViewModel
            {
                AccountId = account.AccountId,
                Email = account.Email,
                Password = account.Password,
                AccountType = account.AccountType
            };
            return Ok(accountViewModel);
        }

        // POST /<AccountController>/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest register)
        {
            if (register == null || string.IsNullOrEmpty(register.Email) || string.IsNullOrEmpty(register.Password))
                return BadRequest(new { success = false, message = "Invalid data" });
            if (_context.Accounts.Any(x => x.Email == register.Email))
                return BadRequest(new { success = false, message = "Email already exists" });

            var salt = SecurityService.GenerateSalt();
            Account account = new Account
            {
                Email = register.Email,
                Password = SecurityService.HashPasswordWithSalt(register.Password, salt),
                Salt = salt,
                AccountType = register.AccountType
            };

            _context.Accounts.Add(account);
            _context.SaveChanges();
            var newAccount = _context.Accounts.FirstOrDefault(x => x.Email == register.Email);
            var profile = new Profile
            {
                AccountId = newAccount.AccountId,
                FirstName = register.FirstName,
                LastName = register.LastName,
                Username = register.Username,
                Avatar = register.Avatar,
                Coverphoto = register.Coverphoto,
                Identifier = register.Identifier
            };

            _context.Profiles.Add(profile);
            _context.SaveChanges();
            return Ok(new
            {
                success = true,
                data = new AccountViewModel
                {
                    AccountId = account.AccountId,
                    Email = account.Email,
                    Password = account.Password,
                    AccountType = account.AccountType
                }
            });
        }

        // POST /<AccountController>/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                return BadRequest(new { success = false, message = "Invalid data" });

            Account account = _context.Accounts.FirstOrDefault(x => x.Email == loginRequest.Email);
            if (account == null)
                return NotFound(new { success = false, message = "Account not found" });

            if (account.Password != SecurityService.HashPasswordWithSalt(loginRequest.Password, account.Salt))
                return BadRequest(new { success = false, message = "Incorrect password" });

            return Ok(new
            {
                success = true,
                data = new AccountViewModel
                {
                    AccountId = account.AccountId,
                    Email = account.Email,
                    Password = account.Password,
                    AccountType = account.AccountType
                }
            });
        }


        [HttpHead("{email}")]
        [HttpGet("{email}/exists")]
        public async Task<IActionResult> CheckEmailExists(string email)
        {
            try
            {
                // Kiểm tra tham số email
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest();
                }

                // Kiểm tra định dạng email
                if (!IsValidEmail(email))
                {
                    return BadRequest();
                }

                // Kiểm tra email trong database
                bool emailExists = await _context.Accounts.AnyAsync(u => u.Email == email.ToLower());

                if (emailExists)
                {
                    // Email đã tồn tại - trả về 200 OK với thông tin tồn tại
                    return Ok();
                }
                else
                {
                    // Email chưa tồn tại - trả về 204 No Content
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
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
