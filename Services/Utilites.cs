namespace BookMoth_Api_With_C_.Services
{
    public class Utilites
    {
        public static bool IsValidEmail(string email)
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

        public static (string FirstName, string LastName) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("", "");

            var parts = fullName.Trim().Split(' ');

            if (parts.Length == 1)
                return (parts[0], "");

            string firstName = parts[^1];
            string lastName = string.Join(" ", parts[..^1]);

            return (firstName, lastName);
        }
    }
}
