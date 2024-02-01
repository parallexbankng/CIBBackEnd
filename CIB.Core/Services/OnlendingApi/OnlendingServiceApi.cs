using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CIB.Core.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CIB.Core.Services.OnlendingApi.Dto
{
    public class OnlendingServiceApi : IOnlendingServiceApi
    {

        private readonly IConfiguration _config;
        private readonly IHttpClientFactory httpClient;
        protected string UserName;
        protected string Password;
        public OnlendingServiceApi(IConfiguration config, IHttpClientFactory client)
        {
            _config = config;
            httpClient = client;
        }
        public async Task<OnlendingAuthTokenResponse> GetAuthToken()
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var data = JsonConvert.SerializeObject(new
            {
                username = Encryption.DecryptStrings(_config.GetValue<string>("OnlendingAuth:username")),
                password = Encryption.DecryptStrings(_config.GetValue<string>("OnlendingAuth:password")),
            });

            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            var url = _config.GetValue<string>("prodApiUrl:onlendingAuth");
            var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(response))
            {
                return new OnlendingAuthTokenResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "Error Authenticating OnLending API User" };
            }
            return JsonConvert.DeserializeObject<OnlendingAuthTokenResponse>(response);
        }

        public async Task<OnlendingAccountOpeningResponse> AccountOpening(OnlendingBeneficiaryAccountOpeningRequest request)
        {

            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:accountOpening");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingAccountOpeningResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending Account Opening API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingAccountOpeningResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingAccountOpeningResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnleandingResponse> CalculateInterest(OnlendingGetInterestRequest request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:getInterest");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending Intrest Calculation API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandingResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnleandingDesbursmentResponse> InitiateBeneficiaryDisbursment(OnlendingInitiateBeneficiaryDisburstment request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:initiateDisburstment");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandingDesbursmentResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending INITIATE DISBURSEMENT API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandingDesbursmentResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandingDesbursmentResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnleandingResponse> InitiateMatchantDisbursment(OnlendingInitiateMerchantDisburstment request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:accountOpening");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending INITIATE DISBURSEMENT API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandingResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnlendingDateExtendsionResponse> InitiateRepaymentDateExtension(OnlendingInitiateExtensionRequest request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:initiateExtension");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingDateExtendsionResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending LOAN EXTENSION API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingDateExtendsionResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingDateExtendsionResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnlendingLiquidationResponse> LiquidatePayment(OnlendingFullLiquidationRequest request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:preLiquidation");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingLiquidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending LIQUIDATION API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingLiquidationResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingLiquidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnlendingLiquidationResponse> PreliquidatePayment(OnlendingPreLiquidationRequest request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:preLiquidation");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingLiquidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending PRE-LIQUIDATION API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingLiquidationResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingLiquidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnleandingResponse> ValidateManagmentFee(OnlendingValidateManagementFeeRequest request)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:accountOpening");
            var data = JsonConvert.SerializeObject(request);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending VALIDATE MANAGEMENT FEE API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandingResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandingResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnleandBvnValidationResponse> ValidateBvn(string Bvn)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var url = _config.GetValue<string>("prodApiUrl:validateBvn");
            var data = JsonConvert.SerializeObject(Bvn);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandBvnValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending Bvn validation API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandBvnValidationResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandBvnValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnleandBvnValidationResponse> TestValidateBvn(string Bvn)
        {
            var _httpClient = httpClient.CreateClient("testClient");
            var url = _config.GetValue<string>("TestClient:bvn");
            try
            {
                var response = await _httpClient.GetAsync(url).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandBvnValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending Bvn validation API Not reachable" };
                }
                var result = JsonConvert.DeserializeObject<List<OnleandBvnValidationResponse>>(response);
                var resultResponse = result.Where(ctx => ctx.Bvn == Bvn).FirstOrDefault();
                if (resultResponse == null)
                {
                    return new OnleandBvnValidationResponse
                    {
                        ResponseCode = "02",
                        ResponseMessage = "Invalid Bvn"
                    };
                }
                return resultResponse;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandBvnValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnleandIdIssueValidationResponse> ValidateNIN(string ninNumber)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var url = _config.GetValue<string>("prodApiUrl:validateNin");
            var data = JsonConvert.SerializeObject(ninNumber);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandIdIssueValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending Bvn validation API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandIdIssueValidationResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandIdIssueValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<OnleandIdIssueValidationResponse> ValidatePassport(string passportId)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var url = _config.GetValue<string>("prodApiUrl:validatePassport");
            var data = JsonConvert.SerializeObject(passportId);
            var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnleandIdIssueValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = "API ERROR, Onlending Bvn validation API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnleandIdIssueValidationResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnleandIdIssueValidationResponse { ResponseCode = ResponseCode.API_ERROR, ResponseMessage = message };
            }
        }

        public async Task<BeneficiaryAdditionInfoRespons> TestGetBeneficiaryAddressInfo()
        {
            var _httpClient = httpClient.CreateClient("testClient");
            var url = _config.GetValue<string>("TestClient:beneficiaryInfo");
            try
            {
                var response = await _httpClient.GetAsync(url).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new BeneficiaryAdditionInfoRespons { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending Bvn validation API Not reachable" };
                }
                var resultResponse = JsonConvert.DeserializeObject<BeneficiaryAdditionInfoRespons>(response);
                if (resultResponse == null)
                {
                    return new BeneficiaryAdditionInfoRespons
                    {
                        ResponseCode = "02",
                        ResponseDescription = "Invalid Data"
                    };
                }
                return resultResponse;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new BeneficiaryAdditionInfoRespons { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnlendingMerchantBeneficiaryResponse> GetMerchantBeneficiaries(string merchantAccountNumber)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:getMerchantBeneficairies");
            try
            {
                var response = await _httpClient.GetAsync(url + $"?merchantAccountNumber={merchantAccountNumber}").Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingMerchantBeneficiaryResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending PRE-LIQUIDATION API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingMerchantBeneficiaryResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingMerchantBeneficiaryResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }

        public async Task<OnlendingMerchantResponse> GetMerchant(string customerId)
        {
            var _httpClient = httpClient.CreateClient("tokenClient");
            var authResult = await GetAuthToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
            var url = _config.GetValue<string>("prodApiUrl:getMerchantById");
            var postUrl = url + $"?custumerId={customerId}";
            try
            {
                var response = await _httpClient.GetAsync(postUrl).Result.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(response))
                {
                    return new OnlendingMerchantResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = "API ERROR, Onlending Merchant API Not reachable" };
                }
                return JsonConvert.DeserializeObject<OnlendingMerchantResponse>(response);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new OnlendingMerchantResponse { ResponseCode = ResponseCode.API_ERROR, ResponseDescription = message };
            }
        }


    }
}