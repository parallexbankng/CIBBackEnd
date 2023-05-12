using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace CIB.CorporateAdmin.Extensions
{
    public static class ApiKeyExtension
    {
        public static IApplicationBuilder UseApiKey(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        //    private readonly ILogger _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public ApiKeyMiddleware(RequestDelegate next, TokenValidationParameters tokenValidationParameters)
        {
            _next = next;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task Invoke(HttpContext context)
        {
            const string header = "Authorization";
            const bool isBearer = true;
            string authorization = context.Request.Headers[header];
            if (!string.IsNullOrEmpty(authorization))
            {
                var token = isBearer ? authorization.Replace("Bearer", "").Trim() : authorization.Trim();
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var claimPrincipal = handler.ValidateToken(token, _tokenValidationParameters, out var securityToken);
                    Thread.CurrentPrincipal = claimPrincipal;
                    context.Items.Add("token", token);
                    context.User = claimPrincipal;
                }
                catch (Exception)
                {
                }
            }
            await _next.Invoke(context);
        }
    }
}