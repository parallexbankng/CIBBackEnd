using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CIB.Core.Modules.Authentication.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace CIB.Core.Utils
{
    public static class TokenGenerator
    {
        private static readonly RNGCryptoServiceProvider provider = new();
        public static string GenerateJSONWebToken(CorporateUserModel userInfo,IConfiguration _config)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Encryption.DecryptStrings(_config["Jwt:Key"])));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim("phone", userInfo.Phone1),
                new Claim("CustomerID", userInfo.CustomerID ?? ""),
                new Claim("Fullname", userInfo.FullName ?? ""),
                new Claim("CorporateCustomerId", userInfo.CorporateCustomerId ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = Encryption.DecryptStrings(_config["Jwt:Issuer"]),
                Audience = Encryption.DecryptStrings(_config["Jwt:Issuer"]),
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(120),
                SigningCredentials = credentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateOTP()
        {
            // Random _rdm = new Random();
            // var numb = _rdm.Next(min, max);
            // var otp = numb.ToString().PadLeft(length, '0');
            // return otp;
            return GenerateToken(6);
        }


       
        public static string GenerateToken(int PasswordLength)
        {
            int PasswordAmount = 1;
            string CapitalLetters = "QWERTYUIOPASDFGHJKLZXCVBNM";
            string Digits = "0123456789";
            string AllChar = CapitalLetters + Digits ;

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

            return sb.ToString();
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