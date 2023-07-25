using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CIB.Core.Services.Email.Dto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CIB.Core.Services.Email
{
  public class EmailService : IEmailService
  {
    private readonly IConfiguration _config;
    public HttpClient _httpClient;
    public EmailService(IConfiguration config, HttpClient client)
    {
      _config = config;
      _httpClient = client;

    }
    public async Task<EmailResponseDto> SendEmail(EmailRequestDto mail)
    {
      var data = JsonConvert.SerializeObject(new
      {
        Sender = mail.sender,
        mail.recipient,
        Subject = mail.subject,
        Message = mail.message,
        IsHtml = true,
      });
      var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
      var url = _config.GetValue<string>("EmailUrl");
      var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
      var result = JsonConvert.DeserializeObject<EmailResponseDto>(response);
      if (result.ResponseCode != "00")
      {
        result.IsSuccess = false;
        return result;
      }
      result.IsSuccess = true;
      return result;
    }
  }
}