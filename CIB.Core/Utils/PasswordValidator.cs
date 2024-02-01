using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CIB.Core.Utils
{
    public static class PasswordValidator
    {
        private static readonly RNGCryptoServiceProvider provider = new();
        public static string ValidatePassword(string password)
        {
            string errormsg = string.Empty;
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMinimum8Chars = new Regex(@".{8,}");
            var hasSpecialCharacters = password.Any(ch => !Char.IsLetterOrDigit(ch));

            if (!hasNumber.IsMatch(password))
            {
                errormsg = "New password must contain at least one NUMERIC alphabet";
            }

            if (!hasUpperChar.IsMatch(password))
            {
                errormsg = "New password must contain at least one UPPERCASE alphabet";
            }

            if (!hasMinimum8Chars.IsMatch(password))
            {
                errormsg = "New password must be at least 8 characters long";
            }

            if (!hasSpecialCharacters)
            {
                errormsg = "New password must have at least one SPECIAL charater";
            }

            //var isValidated = hasNumber.IsMatch(password) && hasUpperChar.IsMatch(password) && hasMinimum8Chars.IsMatch(password);
            return errormsg;
        }
        public static string GeneratePassword()
        {
            return NumberGenerator(10);
        }
        public static string NumberGenerator(int PasswordLength)
        {
            int PasswordAmount = 1;
            string CapitalLetters = "QWERTYUIOPASDFGHJKLZXCVBNM";
            string SmallLetters = "qwertyuiopasdfghjklzxcvbnm";
            string Digits = "0123456789";
            string SpecialCharacters = "!@#$%^&*()-_=+,.";
            string AllChar = CapitalLetters + SmallLetters + Digits + SpecialCharacters;

            string[] AllPasswords = new string[PasswordAmount];

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < PasswordAmount; i++)
            {
                //StringBuilder sb = new StringBuilder();
                for (int n = 0; n < PasswordLength; n++)
                {
                    sb = sb.Append(GenerateChar(AllChar));
                }

                AllPasswords[i] = sb.ToString();
            }

            return "Pb_9" + sb.ToString();
        }
        private static char GenerateChar(string availableChars)
        {
            var byteArray = new byte[1];
            char c;
            do
            {
                provider.GetBytes(byteArray);
                c = (char)byteArray[0];

            } 
            while (!availableChars.Any(x => x == c));
            return c;
        }
    }
}