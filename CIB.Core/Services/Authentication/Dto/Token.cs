using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.Authentication.Dto
{
    public class Tokens
    {
        public string Token { get; set; }
	    public string RefreshToken { get; set; }
    }
    public class Token
    {
        public string access_token { get; set; }
	    public string refresh_token { get; set; }
    }
}