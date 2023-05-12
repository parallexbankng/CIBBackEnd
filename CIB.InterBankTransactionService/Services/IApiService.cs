using CIB.InterBankTransactionService.Services.Request;
using CIB.InterBankTransactionService.Services.Response;

namespace CIB.InterBankTransactionService.Services;

public interface IApiService
{
  Task<TransferResponse> PostInterBankTransfer(PostInterBankTransaction transaction);
  AuthTokenResponse GetAuthToken();
}
