using CIB.TransactionReversalService.Services.Request;
using CIB.TransactionReversalService.Services.Response;

namespace CIB.TransactionReversalService.Services;

public interface IApiService
{
  Task<TransferResponse> PostIntraBankTransfer(PostIntraBankTransaction transaction);
  AuthTokenResponse GetAuthToken();
  Task<CustomerDataResponseDto> GetCustomerDetailByAccountNumber(string accountNumber);
}
