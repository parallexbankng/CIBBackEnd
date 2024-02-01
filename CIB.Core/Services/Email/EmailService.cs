
using System.Net.Http;
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
		private readonly IHttpClientFactory httpClient;
		public EmailService(IConfiguration config, IHttpClientFactory client)
		{
			_config = config;
			httpClient = client;

		}
		public async Task<EmailResponseDto> SendEmail(EmailRequestDto mail)
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var data = JsonConvert.SerializeObject(new
			{
				Sender = mail.sender,
				mail.recipient,
				Subject = mail.subject,
				Message = mail.message,
				IsHtml = true,
			});
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			var url = _config.GetValue<string>("prodApiUrl:EmailUrl");
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