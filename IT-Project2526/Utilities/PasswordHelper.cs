using System.Security.Cryptography;
using System.Text;

namespace IT_Project2526.Utilities
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Generates a secure random password that meets ASP.NET Identity requirements
        /// </summary>
        /// <param name="length">Length of password (default 12)</param>
        /// <returns>Secure password string</returns>
        public static string GenerateSecurePassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%";
            var result = new StringBuilder(length);
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[sizeof(uint)];
                
                while (result.Length < length)
                {
                    rng.GetBytes(buffer);
                    var num = BitConverter.ToUInt32(buffer, 0);
                    result.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }
            
            // Ensure it meets complexity requirements (digit, uppercase, special char)
            var password = result.ToString();
            if (!HasDigit(password))
                result[0] = '1';
            if (!HasUpperCase(password))
                result[1] = 'A';
            if (!HasSpecialChar(password))
                result[2] = '!';
            
            return result.ToString();
        }
        
        /// <summary>
        /// Generates a simple welcome password for new customers
        /// Format: Welcome{Year}!
        /// </summary>
        public static string GenerateWelcomePassword()
        {
            return $"Welcome{DateTime.Now.Year}!";
        }
        
        private static bool HasDigit(string password) 
            => password.Any(char.IsDigit);
        
        private static bool HasUpperCase(string password) 
            => password.Any(char.IsUpper);
        
        private static bool HasSpecialChar(string password) 
            => password.Any(c => "!@#$%^&*()".Contains(c));
    }
}
