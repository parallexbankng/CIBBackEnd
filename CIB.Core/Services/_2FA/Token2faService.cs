using System;
using System.Net.Http;
using System.Threading.Tasks;
using CIB.Core.Services._2FA.Dto;
using CIB.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.Core.Services._2FA
{
  public class Token2faService : IToken2faService
  {
    private  readonly IConfiguration _config;
    private readonly IHttpClientFactory httpClient;
    private readonly ILogger<Token2faService> _logger;
    public Token2faService (IConfiguration config,IHttpClientFactory client,ILogger<Token2faService> logger){
      _config = config;
      _logger = logger;
      httpClient = client;
    }
    public async Task<_2faResponseDto> TokenAuth(string UserName, string Token)
    {
      try
      {
        var myUserName = UserName.Trim().ToLower();
        var myToken = Token.Trim();
        var _httpClient = httpClient.CreateClient("tokenClient");
        var url = _config.GetValue<string>("prodApiUrl:entrustToken");
        var response = await _httpClient.GetAsync(url+$"?UserId={myUserName}&tokenResponse={myToken}").Result.Content.ReadAsStringAsync();
        if(string.IsNullOrEmpty(response))
        {
          return new _2faResponseDto{ResponseCode = "17", ResponseMessage = $"2FA API Failed Error"};
        }
        return JsonConvert.DeserializeObject<_2faResponseDto>(response);
      }
      catch (Exception ex)
      {
        var errorItem = ex.InnerException != null ?  new _2faResponseDto{ResponseCode=ResponseCode.API_ERROR, ResponseMessage= ex.InnerException.Message} : new _2faResponseDto{ResponseCode=ResponseCode.API_ERROR, ResponseMessage= ex.InnerException != null ? ex.InnerException.Message : ex.Message};
        _logger.LogError("2FA API ERROR {0}", Formater.JsonType(errorItem));
        return errorItem;
      }
  }
  }
}