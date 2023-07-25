using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Authentication.Dto;
using CIB.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Extensions
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _config;
        public AuthenticationService(IConfiguration iconfiguration)
        {
            this._config = iconfiguration;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public Tokens JWTAuthentication(CorporateUserModel userInfo, BankUserLoginParam bankuse)
        {
            var payLoadData = JsonConvert.SerializeObject(new {phone=userInfo.Phone1,Email =userInfo.Email, UserName = userInfo.Username});
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Encryption.DecryptStrings(_config["Jwt:Key"])));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
                new Claim("data", Encryption.EncryptStrings(payLoadData))
            };
        
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = Encryption.DecryptStrings(_config["Jwt:Issuer"]),
                Audience = Encryption.DecryptStrings(_config["Jwt:Audience"]),
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = credentials,
                
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new Tokens { Token = tokenHandler.WriteToken(token)};
        }

        public  ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Encryption.DecryptStrings(_config["Jwt:Issuer"]),
                ValidAudience = Encryption.DecryptStrings(_config["Jwt:Issuer"]),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Encryption.DecryptStrings(_config["Jwt:Key"])))
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;
                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)) return null;
                return principal;
            }
            catch (Exception)
            {
                
                return null;
            }
        }

        
        public string ComputeContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashedBytes);
        }

        public string ComputeSignature(string stringToSign)
        {
            string secret = Encryption.DecryptStrings(_config["Jwt:key"]);
            using var hmacsha256 = new HMACSHA256(Convert.FromBase64String(secret));
            var bytes = Encoding.UTF8.GetBytes(stringToSign);
            var hashedBytes = hmacsha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
