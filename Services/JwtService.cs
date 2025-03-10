using BookMoth_Api_With_C_.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BookMoth_Api_With_C_.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _securityKey;
        private readonly int _accessTokenExpiresInMinutes;
        private readonly int _refreshTokenExpiresInMonths;

        public JwtService(IConfiguration config)
        {
            _config = config;
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing")));
            _accessTokenExpiresInMinutes = int.TryParse(_config["Jwt:AccessTokenExpiresIn"], out int expires) ? expires : 120;
            _refreshTokenExpiresInMonths = int.TryParse(_config["Jwt:RefreshTokenExpiresIn"], out int refreshTokenExpires) ? refreshTokenExpires : 1440;
        }

        public string GenerateSecurityToken(Account account)
        {
            var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim("accountId", account.AccountId.ToString())
            };

            //if (!string.IsNullOrEmpty(account.Role))
            //{
            //    claims.Add(new Claim(ClaimTypes.Role, account.Role));
            //}

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiresInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}
