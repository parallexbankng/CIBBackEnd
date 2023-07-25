using System.Security.Claims;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Services.Authentication.Dto;

namespace CIB.Core.Services.Authentication
{
    public interface IAuthenticationService
    {
        Tokens JWTAuthentication(CorporateUserModel? userInfo=null,BankUserLoginParam? bankuse = null);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}