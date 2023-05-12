using CIB.IntraBankTransactionService.Services.Request;
using CIB.IntraBankTransactionService.Services.Response;

namespace CIB.IntraBankTransactionService.Services;

public interface IApiService
{
  Task<TransferResponse> PostIntraBankTransfer(PostIntraBankTransaction transaction);
  AuthTokenResponse GetAuthToken();
}
