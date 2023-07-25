using System.Net.Http.Headers;
using System.Text;
using CIB.InterBankTransactionService.Services.Request;
using CIB.InterBankTransactionService.Services.Response;
using CIB.InterBankTransactionService.Utils;
using Newtonsoft.Json;

namespace CIB.InterBankTransactionService.Services;

public class ApiService : IApiService
{
  private readonly IConfiguration _config;
  private readonly IHttpClientFactory _client;
  private readonly  AuthTokenResponse _token;
  private readonly ILogger<ApiService> _logger;
  
  public ApiService (IConfiguration config,IHttpClientFactory client,ILogger<ApiService> logger){
    _config = config;
    _client = client;
    _logger = logger;
    _token = this.GetAuthToken();
  }
  public async Task<TransferResponse> PostInterBankTransfer(PostInterBankTransaction transaction)
  {
    if(_token.ResponseCode != "00")
    {
      return new TransferResponse();
    }
    var httpClient = _client.CreateClient("tokenClient");
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.Token);
    var data = JsonConvert.SerializeObject(transaction);
    var url = _config.GetValue<string>("TestApiUrl:interBankTransfer");
    var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<TransferResponse>(response);
  }
  public AuthTokenResponse GetAuthToken()
  {
    var httpClient = _client.CreateClient("tokenClient");
    var data = JsonConvert.SerializeObject(new
    {
      username = Encryption.DecryptStrings(_config.GetValue<string>("DefaultAuth:username")),
      Password = Encryption.DecryptStrings(_config.GetValue<string>("DefaultAuth:password")),
    });
    var url = _config.GetValue<string>("TestApiUrl:auth");
    var request = new HttpRequestMessage(HttpMethod.Post, url);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    request.Content  = new StringContent(data, Encoding.UTF8);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    var response = httpClient.Send(request);
    response.EnsureSuccessStatusCode();
    var content = response.Content.ReadAsStringAsync().Result;
    return JsonConvert.DeserializeObject<AuthTokenResponse>(content);
  }

  public async Task<RequeryTransactionResponse> QueryTransferTransaction(RequeryTransaction query)
  {
    var _httpClient = _client.CreateClient("tokenClient");
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.Token);
    var url = _config.GetValue<string>("TestApiUrl:queryTransaction");
    var data = JsonConvert.SerializeObject(query);
    var payLoad = new StringContent(data, Encoding.UTF8, "application/json");
    try
    {
      var response = await _httpClient.PostAsync(url, payLoad).Result.Content.ReadAsStringAsync();
      if(string.IsNullOrEmpty(response))
      {
        return new RequeryTransactionResponse { ResponseCode = "15", ResponseMessage = "API Error Return Null" };
      }
      return JsonConvert.DeserializeObject<RequeryTransactionResponse>(response);
    }
    catch(Exception ex)
    {
      var errorItem = ex.InnerException != null ?  new RequeryTransactionResponse{ResponseCode = "14", ResponseMessage= ex.InnerException.Message } : new RequeryTransactionResponse{ResponseCode ="500", ResponseMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message};
      _logger.LogError("API ERROR {0}", JsonConvert.SerializeObject(errorItem));
      return errorItem;
    } 
  }
}
