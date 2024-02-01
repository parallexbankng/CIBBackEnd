using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CIB.Core.Services.Api.Dto;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Modules.Transaction.Dto.Interbank;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Utils;
using CIB.Core.Modules.Transaction.Dto;
using Microsoft.Extensions.Logging;

namespace CIB.Core.Services.Api
{
	public class ApiService : IApiService
	{
		private readonly IConfiguration _config;
		private readonly IHttpClientFactory httpClient;
		protected string UserName;
		protected string Password;
		private readonly ILogger<ApiService> _logger;
		public ApiService(IConfiguration config, IHttpClientFactory client, ILogger<ApiService> logger)
		{
			_config = config;
			httpClient = client;
			_logger = logger;
		}
		public async Task<AuthTokenResponse> GetAuthToken()
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var data = JsonConvert.SerializeObject(new
			{
				username = Encryption.DecryptStrings(_config.GetValue<string>("DefaultAuth:username")),
				password = Encryption.DecryptStrings(_config.GetValue<string>("DefaultAuth:password")),
			});

			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			var url = _config.GetValue<string>("prodApiUrl:auth");
			var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new AuthTokenResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "Error Authenticating User Bank User" };
			}
			return JsonConvert.DeserializeObject<AuthTokenResponse>(response);
		}
		public async Task<CustomerDataResponseDto> GetCustomerDetailByAccountNumber(string accountNumber)
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var authResult = await GetAuthToken();
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
			var url = _config.GetValue<string>("prodApiUrl:accountInfo");
			var response = await _httpClient.GetAsync(url + $"?accountNumber={accountNumber}&bankId=01").Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new CustomerDataResponseDto { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error Get Customer Details By Account Request Failed" };
			}
			return JsonConvert.DeserializeObject<CustomerDataResponseDto>(response);
		}
		public async Task<IntraBankTransferResponse> IntraBankTransfer(IntraBankPostDto transaction)
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var authResult = await GetAuthToken();
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
			var data = JsonConvert.SerializeObject(transaction);
			var url = _config.GetValue<string>("prodApiUrl:intraBankTransfer");
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new IntraBankTransferResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error Intra bank Transfer Request Failed" };
			}
			return JsonConvert.DeserializeObject<IntraBankTransferResponse>(response);
		}
		public async Task<IntraBankTransferResponse> InterBankTransfer(InterBankPostDto transaction)
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var authResult = await GetAuthToken();
			var data = JsonConvert.SerializeObject(transaction);
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
			var url = _config.GetValue<string>("prodApiUrl:interBankTransfer");
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new IntraBankTransferResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error Inter bank Transfer Request Failed" };
			}
			return JsonConvert.DeserializeObject<IntraBankTransferResponse>(response);
		}

		public async Task<BankListResponseData> GetBanks()
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var url = _config.GetValue<string>("prodApiUrl:getBank");
			var response = await _httpClient.GetAsync(url).Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new BankListResponseData { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API Error Name Enquire with bank code Failed" };
			}
			return JsonConvert.DeserializeObject<BankListResponseData>(response);
		}
		public async Task<InterbankNameEnquiryResponseDto> BankNameInquire(string accountNumber, string bankCode)
		{
			var _httpClient = httpClient.CreateClient("finnacleClient");
			var url = _config.GetValue<string>("prodApiUrl:interBankNameEnquire");
			var response = await _httpClient.GetAsync(url + $"?accountNumber={accountNumber}&BankCode={bankCode}&bankId=01").Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new InterbankNameEnquiryResponseDto { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API Error Name Enquire with bank code Failed" };
			}
			return JsonConvert.DeserializeObject<InterbankNameEnquiryResponseDto>(response);
		}
		public async Task<RelatedCustomerAccountDetailsDto> RelatedCustomerAccountDetails(string CustomerId)
		{
			try
			{
				string jsonString = string.Empty;
				string errormsg = string.Empty;
				var nameEnqResponseData = new RelatedCustomerAccountDetailsDto();
				var _httpClient = httpClient.CreateClient("finnacleClient");
				var url = _config.GetValue<string>("prodApiUrl:getAccountsByCustomerId");
				var requestUrl = url + $"?CustId={CustomerId}&bankId=01";
				var response = await _httpClient.GetAsync(requestUrl);

				if (response?.IsSuccessStatusCode == true)
				{
					jsonString = await response.Content.ReadAsStringAsync();
					nameEnqResponseData = JsonConvert.DeserializeObject<RelatedCustomerAccountDetailsDto>(jsonString);
				}
				else
				{
					jsonString = string.Empty;
					try
					{
						jsonString = await response.Content.ReadAsStringAsync();
						nameEnqResponseData = JsonConvert.DeserializeObject<RelatedCustomerAccountDetailsDto>(jsonString);
					}
					catch (Exception ex)
					{
						string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
						_logger.LogError($"{"RelatedCustomerAccountDetails"}: Request was unsuccessful for customer related account. status code is " + $"{response?.StatusCode}. Exception is {errormsg}, Json response is {jsonString}");
						//errormsg = $"{err}: code: {response?.StatusCode}";
						errormsg = "Unavailable service";
					}
				}

				if (nameEnqResponseData != null)
				{
					if (nameEnqResponseData.RespondCode != "00")
					{
						return new RelatedCustomerAccountDetailsDto { RespondCode = ResponseCode.API_ERROR, RespondMessage = $"API Error {nameEnqResponseData.RespondMessage}" };
					}
					return nameEnqResponseData;
				}
				else
				{
					_logger.LogInformation($"{"RelatedCustomerAccountDetails"}: Customer related account failure for customer number: {CustomerId}. status code is: {response?.StatusCode} response status is: {response?.RequestMessage}, json string: {jsonString}");
					//errormsg = $"{nameEnqResponseData?.StatusCode} {nameEnqResponseData?.ResponseStatus} Request to the service was unsuccessful";
					errormsg = $"Unavailable service. {response?.StatusCode}";
				}

				return nameEnqResponseData;

			}
			catch (Exception ex)
			{
				string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
				_logger.LogInformation($"{RelatedCustomerAccountDetails}: Customer related account failure for customer number: {CustomerId}. error: {err}");
				//errormsg = "Request to the service was unsuccessful";
				return new RelatedCustomerAccountDetailsDto { RespondCode = ResponseCode.API_ERROR, RespondMessage = $"Unavailable service" };
			}
		}

		public async Task<CustomerDataResponseDto> CustomerNameInquiry(string accountNumber)
		{

			var authResult = await GetAuthToken();
			var _httpClient = httpClient.CreateClient("tokenClient");
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
			var url = _config.GetValue<string>("prodApiUrl:accountInfo");
			var response = await _httpClient.GetAsync(url + $"?accountNumber={accountNumber}&bankId=01").Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new CustomerDataResponseDto { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error Name Enquiry Failed" };
			}
			return JsonConvert.DeserializeObject<CustomerDataResponseDto>(response);
		}
		public async Task<StatementOfAccountResponseDto> GenerateStatement(StatementOfAccountRequestDto accountRequestDto)
		{
			var _httpClient = httpClient.CreateClient("finnacleClient");
			var data = JsonConvert.SerializeObject(accountRequestDto);
			var url = _config.GetValue<string>("prodApiUrl:generateStatement");
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new StatementOfAccountResponseDto { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error fetching Account Statement" };
			}
			return JsonConvert.DeserializeObject<StatementOfAccountResponseDto>(response);
		}
		public async Task<AdUserInfo> ADBasicInfoInquire(string UserName)
		{
			var _httpClient = httpClient.CreateClient("tokenClient");
			var url = _config.GetValue<string>("prodApiUrl:adUserInfoEnquire");
			var response = await _httpClient.GetAsync(url + $"?username={UserName}").Result.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(response))
			{
				return new AdUserInfo { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API Error Authenticating User With AD" };
			}
			return JsonConvert.DeserializeObject<AdUserInfo>(response);
		}
		public async Task<ADLoginResponseDto> ADLogin(string UserName, string Password)
		{
			var bankUserName = UserName.ToLower().Trim();
			var bankPassword = Password.Trim();
			var _httpClient = httpClient.CreateClient("adClient");
			var url = _config.GetValue<string>("prodApiUrl:adUserAuthentication");

			var payLoadData = JsonConvert.SerializeObject(new { UserName = $"{bankUserName}", Password = $"{bankPassword}" });
			var data = JsonConvert.SerializeObject(new { Data = Encryption.EncryptStrings(payLoadData) });
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			try
			{
				var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
				if (string.IsNullOrEmpty(response))
				{
					return new ADLoginResponseDto { IsAuthenticated = false, ResponseMessage = "Error Authenticating User With AD" };
				}
				var result = JsonConvert.DeserializeObject<ADData>(response);
				return JsonConvert.DeserializeObject<ADLoginResponseDto>(Encryption.DecryptStrings(result.Data));
			}
			catch (Exception ex)
			{
				var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
				return new ADLoginResponseDto { IsAuthenticated = false, ResponseMessage = message };
			}
		}
		public async Task<BulkIntraBankTransactionResponse> IntraBankBulkTransfer(BulkIntrabankTransactionModel transaction)
		{
			var _httpClient = httpClient.CreateClient("finnacleClient");
			var url = _config.GetValue<string>("prodApiUrl:bulkTransaction");
			var data = JsonConvert.SerializeObject(transaction);
			var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
			try
			{
				var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
				if (string.IsNullOrEmpty(response))
				{
					return new BulkIntraBankTransactionResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "Error Posting Bulk Intrabank Transaction" };
				}
				return JsonConvert.DeserializeObject<BulkIntraBankTransactionResponse>(response);
			}
			catch (Exception ex)
			{
				var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
				return new BulkIntraBankTransactionResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
			}
		}
	}
}


