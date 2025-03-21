using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.ResponseModels;
using BookMoth_Api_With_C_.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Globalization;
using RegisterRequest = BookMoth_Api_With_C_.RequestModels.RegisterRequest;



namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private string url = "http://127.0.0.1:7100/images/";
        private BookMothContext _context;
        private IMemoryCache _cache;
        private EmailService _emailService;
        private JwtService _jwtService;

        public AccountController(
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


            var accId = int.Parse(accountId);

            var account = await _context.Accounts.FirstOrDefaultAsync(x => x.AccountId == accId);
            if (account == null)
            {
                return NotFound(new { message = "Account not found" });
            }

            return Ok(new
            {
                accountId = account.AccountId,
                email = account.Email
            });
        }

        [HttpPost("auth/google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(u =>
                u.Email == payload.Email && u.AccountType == 1);
            if (account == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var jwtToken = _jwtService.GenerateSecurityToken(account);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var hashedToken = _jwtService.HashToken(refreshToken);

            var oldToken = _context.RefreshTokens.FirstOrDefault(
                x => x.AccountId == account.AccountId &&
                x.CreatedByIp == HttpContext.Connection.RemoteIpAddress.ToString()
            );

            using (var transaction = _context.Database.BeginTransaction())
            {

                try
                {
                    if (oldToken != null)
                    {
                        _context.RefreshTokens.Remove(oldToken);
                        _context.SaveChanges();
                    }
                    var refresh = new RefreshToken
                    {
                        AccountId = account.AccountId,
                        Token = hashedToken,
                        ExpiryDate = DateTime.UtcNow.AddMonths(_jwtService.RefreshTokenExpiresInMonths),
                        CreatedDate = DateTime.UtcNow,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsActive = true
                    };

                    _context.RefreshTokens.Add(refresh);
                    _context.SaveChanges();
                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            jwtToken = jwtToken,
                            refreshToken = refreshToken
                        }
                    });
                }
                catch
                {
                    transaction.Rollback();
                    return UnprocessableEntity(new { success = false, message = "Error while logging in." });
                }
            }
        }

        [HttpPost("auth/google-register")]
        public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new { message = "Invalid request: IdToken is required" });
            }

            var payload = await VerifyGoogleToken(request.IdToken);
            if (payload == null)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }

            var existAccount = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (existAccount != null)
            {
                return Conflict(new { message = "Account already exists" });
            }
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var salt = SecurityService.GenerateSalt();
                    var account = new Account
                    {
                        Email = payload.Email,
                        Password = SecurityService.HashPasswordWithSalt("", salt),
                        Salt = salt,
                        AccountType = 1
                    };

                    _context.Accounts.Add(account);
                    _context.SaveChanges();

                    var newAccount = _context.Accounts.FirstOrDefault(x => x.Email == payload.Email);
                    if (newAccount == null)
                    {
                        throw new Exception("Account creation failed.");
                    }

                    var count = _context.Accounts.Count();

                    var accessToken = await ExchangeIdTokenForAccessToken(request.IdToken);
                    var googleInfo = await GetGoogleUserInfo(accessToken);

                    var profile = new Profile
                    {
                        AccountId = newAccount.AccountId,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        Username = "member_" + count.ToString(),
                        Avatar = payload.Picture,
                        Coverphoto = "",
                        Identifier = false,
                        Gender = googleInfo.Gender,
                        Birth = googleInfo.BirthDate
                    };
                    _context.Profiles.Add(profile);
                    _context.SaveChanges();

                    var wallet = new Wallet
                    {
                        AccountId = newAccount.AccountId,
                        Balance = 0,
                        Status = 1
                    };

                    _context.Wallets.Add(wallet);
                    _context.SaveChanges();

                    var jwtToken = _jwtService.GenerateSecurityToken(newAccount);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var hashedToken = _jwtService.HashToken(refreshToken);

                    var refresh = new RefreshToken
                    {
                        AccountId = newAccount.AccountId,
                        Token = hashedToken,
                        ExpiryDate = DateTime.UtcNow.AddMonths(_jwtService.RefreshTokenExpiresInMonths),
                        CreatedDate = DateTime.UtcNow,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsActive = true
                    };

                    _context.RefreshTokens.Add(refresh);
                    _context.SaveChanges();

                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            jwtToken = jwtToken,
                            refreshToken = refreshToken
                        }
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return UnprocessableEntity(new { message = "Error while registering account", error = ex.Message });
                }
            }
        }

        private async Task<string> ExchangeIdTokenForAccessToken(string idToken)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/tokeninfo?id_token=" + idToken);
                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                return data["access_token"]?.ToString();
            }
        }

        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { "1097415846771-4ebh7o3h3jo66jbce83q9fkht409frm8.apps.googleusercontent.com" }
                };
                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                return null;
            }
        }

        private async Task<(int Gender, DateTime? BirthDate)> GetGoogleUserInfo(string accessToken)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://people.googleapis.com/v1/people/me?personFields=genders,birthdays");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                string genderStr = data["genders"]?[0]?["value"]?.ToString() ?? "unknown";
                int gender = genderStr.ToLower() switch
                {
                    "male" => 0,  
                    "female" => 1, 
                    _ => 2          
                };

                var birthdateObj = data["birthdays"]?[0]?["date"];
                DateTime? birthDate = null;
                if (birthdateObj != null)
                {
                    int year = birthdateObj["year"]?.ToObject<int>() ?? 0;
                    int month = birthdateObj["month"]?.ToObject<int>() ?? 1;
                    int day = birthdateObj["day"]?.ToObject<int>() ?? 1;

                    if (year > 0)
                    {
                        birthDate = new DateTime(year, month, day);
                    }
                }

                return (gender, birthDate);
            }
        }





        // POST /<AccountController>/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest register)
        {
            if (register == null || string.IsNullOrEmpty(register.Email) || string.IsNullOrEmpty(register.Password))
                return BadRequest(new { success = false, message = "Invalid data" });

            if (_context.Accounts.Any(x => x.Email == register.Email))
                return Conflict(new { success = false, message = "Email already exists" });

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var salt = SecurityService.GenerateSalt();
                    var account = new Account
                    {
                        Email = register.Email,
                        Password = SecurityService.HashPasswordWithSalt(register.Password, salt),
                        Salt = salt,
                        AccountType = register.AccountType
                    };

                    _context.Accounts.Add(account);
                    _context.SaveChanges();

                    var newAccount = _context.Accounts.FirstOrDefault(x => x.Email == register.Email);
                    if (newAccount == null)
                    {
                        throw new Exception("Account creation failed.");
                    }

                    DateTime dateOfBirth = default;

                    if (!DateTime.TryParseExact(register.DateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                    {
                        return BadRequest(new { success = false, message = "Invalid date of birth" });
                    }

                    var count = _context.Accounts.Count();

                    var profile = new Profile
                    {
                        AccountId = newAccount.AccountId,
                        FirstName = register.FirstName,
                        LastName = register.LastName,
                        Username = "member_" + count.ToString(),
                        Avatar = url + "avatar.jpeg",
                        Coverphoto = register.Coverphoto,
                        Identifier = false,
                        Gender = register.Gender,
                        Birth = dateOfBirth
                    };

                    _context.Profiles.Add(profile);
                    _context.SaveChanges();

                    var wallet = new Wallet
                    {
                        AccountId = newAccount.AccountId,
                        Balance = 0,
                        Status = 1
                    };

                    _context.Wallets.Add(wallet);
                    _context.SaveChanges();

                    var jwtToken = _jwtService.GenerateSecurityToken(newAccount);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var hashedToken = _jwtService.HashToken(refreshToken);

                    var refresh = new RefreshToken
                    {
                        AccountId = newAccount.AccountId,
                        Token = hashedToken,
                        ExpiryDate = DateTime.UtcNow.AddMonths(_jwtService.RefreshTokenExpiresInMonths),
                        CreatedDate = DateTime.UtcNow,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsActive = true
                    };

                    _context.RefreshTokens.Add(refresh);
                    _context.SaveChanges();

                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            jwtToken = jwtToken,
                            refreshToken = refreshToken
                        }
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return UnprocessableEntity(new { success = false, message = "Error while registering account.", error = ex.Message });
                }
            }
        }


        // POST /<AccountController>/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email)
                || string.IsNullOrEmpty(loginRequest.Password) || !Utilites.IsValidEmail(loginRequest.Email))
                return BadRequest(new { success = false, message = "Invalid data" });

            var account = _context.Accounts.FirstOrDefault(
                x => x.Email == loginRequest.Email && x.AccountType == 0);
            if (account == null)
                return NotFound(new { success = false, message = "Account not found" });

            if (!SecurityService.HashPasswordWithSalt(loginRequest.Password, account.Salt)
                .Equals(account.Password))
            {
                return Unauthorized(new { success = false, message = "Invalid password" });
            }

            var jwtToken = _jwtService.GenerateSecurityToken(account);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var hashedToken = _jwtService.HashToken(refreshToken);

            var oldToken = _context.RefreshTokens.FirstOrDefault(
                x => x.AccountId == account.AccountId &&
                x.CreatedByIp == HttpContext.Connection.RemoteIpAddress.ToString()
            );

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (oldToken != null)
                    {
                        _context.RefreshTokens.Remove(oldToken);
                        _context.SaveChanges();
                    }
                    var refresh = new RefreshToken
                    {
                        AccountId = account.AccountId,
                        Token = hashedToken,
                        ExpiryDate = DateTime.UtcNow.AddMonths(_jwtService.RefreshTokenExpiresInMonths),
                        CreatedDate = DateTime.UtcNow,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsActive = true
                    };

                    _context.RefreshTokens.Add(refresh);
                    _context.SaveChanges();
                    transaction.Commit();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            jwtToken = jwtToken,
                            refreshToken = refreshToken
                        }
                    });
                }
                catch
                {
                    transaction.Rollback();
                    return UnprocessableEntity(new { success = false, message = "Error while logging in." });
                }
            }

        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
        {
            if (model == null || string.IsNullOrEmpty(model.RefreshToken))
            {
                return BadRequest(new { message = "Invalid refresh token" });
            }

            var hashedToken = _jwtService.HashToken(model.RefreshToken);
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == hashedToken && r.IsActive == true);

            if (refreshToken == null || refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            // Kiểm tra IP nếu cần
            if (refreshToken.CreatedByIp != HttpContext.Connection.RemoteIpAddress?.ToString())
            {
                return Unauthorized(new { message = "Refresh token used from different IP" });
            }

            // Kiểm tra token đã bị thu hồi chưa
            if (refreshToken.RevokedDate != null)
            {
                return Unauthorized(new { message = "Refresh token has been revoked" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == refreshToken.AccountId);
                    if (account == null)
                    {
                        return Unauthorized(new { message = "User not found" });
                    }

                    // Tạo token mới
                    var newJwtToken = _jwtService.GenerateSecurityToken(account);
                    var newRefreshToken = _jwtService.GenerateRefreshToken();
                    var hashedNewToken = _jwtService.HashToken(newRefreshToken);

                    // Tạo refresh token mới
                    var newTokenEntry = new RefreshToken
                    {
                        AccountId = refreshToken.AccountId,
                        Token = hashedNewToken,
                        ExpiryDate = DateTime.UtcNow.AddDays(7),
                        CreatedDate = DateTime.UtcNow,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsActive = true
                    };

                    // Lưu refresh token mới
                    _context.RefreshTokens.Add(newTokenEntry);
                    await _context.SaveChangesAsync();

                    // Xóa token cũ sau khi token mới đã được lưu
                    await _context.RefreshTokens.Where(
                        r => r.AccountId == refreshToken.AccountId &&
                        r.TokenId != newTokenEntry.TokenId)
                        .ExecuteDeleteAsync();

                    await transaction.CommitAsync();

                    return Ok(new { jwtToken = newJwtToken, refreshToken = newRefreshToken });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "An error occurred while refreshing token" });
                }
            }
        }


        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Token không hợp lệ!" });

            var hashedToken = _jwtService.HashToken(request.RefreshToken);
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == hashedToken && x.IsActive);

            if (refreshToken == null)
                return BadRequest(new { message = "Token không tồn tại hoặc đã bị thu hồi!" });


            refreshToken.RevokedDate = DateTime.UtcNow;
            refreshToken.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            refreshToken.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Refresh Token đã được thu hồi thành công!" });
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
                if (!Utilites.IsValidEmail(email))
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

        [HttpDelete("logout")]
        public async Task<IActionResult> logout([FromBody] LogoutRequest logoutRquest)
        {
            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null || logoutRquest == null)
            {
                return BadRequest();
            }

            if (!int.TryParse(accId, out var id))
            {
                return BadRequest();
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
            if (account == null)
            {
                return NotFound();
            }
            // Xóa token cũ sau khi token mới đã được lưu
            await _context.RefreshTokens.Where(
                r => r.AccountId == id)
                .ExecuteDeleteAsync();

            await _context.SaveChangesAsync();

            await _context.FcmTokens.Where(
                r => r.AccountId == id)
                .ExecuteDeleteAsync();
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
