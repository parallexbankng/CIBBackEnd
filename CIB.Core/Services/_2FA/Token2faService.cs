using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CIB.Core.Services._2FA.Dto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CIB.Core.Services._2FA
{
  public class Token2faService : IToken2faService
  {
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory httpClient;
    public Token2faService(IConfiguration config, IHttpClientFactory client)
    {
      _config = config;
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
        var response = await _httpClient.GetAsync(url + $"?UserId={myUserName}&tokenResponse={myToken}").Result.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(response))
        {
          return new _2faResponseDto { ResponseCode = "17", ResponseMessage = $"2FA API Failed Error" };
        }
        return JsonConvert.DeserializeObject<_2faResponseDto>(response);
      }
      catch (Exception ex)
      {
        return new _2faResponseDto { ResponseCode = "17", ResponseMessage = $"2FA API Failed Error  {ex.Message} =>  {ex.StackTrace} " };
      }
    }
  }
}