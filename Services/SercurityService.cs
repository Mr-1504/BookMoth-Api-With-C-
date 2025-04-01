using BookMoth_Api_With_C_.ZaloPay.Crypto;
using System.Security.Cryptography;
using System.Text;

namespace BookMoth_Api_With_C_.Services
{
    public class SecurityService
    {
        public static string GenerateSalt(int size = 32)
        {
            var rng = new RNGCryptoServiceProvider();
            var saltBytes = new byte[size];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string GenerateRandomCode(int length = 6)
        {
            Random random = new Random();
            return string.Join("", Enumerable.Range(0, length).Select(_ => random.Next(0, 10)));
        }

        public static string HashPasswordWithSalt(string password, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                var combinedPassword = password + salt;
                byte[] passwordBytes = Encoding.UTF8.GetBytes(combinedPassword);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string ComputeHashMac(string key = "", string message = "")
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] hashMessage = null;

            hashMessage = new HMACSHA256(keyByte).ComputeHash(messageBytes);

            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
    }
}
