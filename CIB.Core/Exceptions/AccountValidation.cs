using System;
using System.Linq;
using CIB.Core.Services.Api.Dto;

namespace CIB.Core.Exceptions
{
  public static class AccountValidation
  {
    public static bool SourceAccount(CustomerDataResponseDto senderInfo, out string errorMessage)
    {
      if (senderInfo.ResponseCode != "00")
      {
        errorMessage = $"Source account number could not be verified -> {senderInfo.ResponseDescription}";
        return false;
      }
      if (senderInfo.AccountStatus != "A")
      {
        errorMessage = $"Source account is not active transaction cannot be completed ";
        return false;
      }
      if (senderInfo.FreezeCode != "N")
      {
        errorMessage = $"Source account is on debit freeze transaction cannot be completed";
        return false;
      }
      errorMessage = "Ok";
      return true;
    }

    public static bool RelatedAccount(RelatedCustomerAccountDetailsDto account, string AccountNumber, out string errorMessage)
    {
      if (account.RespondCode != "00")
      {
        errorMessage = $"can not verify Source Account Number";
        return false;
      }

      var confirmSourceAccount = account?.Records?.Where(ctx => ctx.AccountNumber == AccountNumber).ToList();
      if (!confirmSourceAccount.Any())
      {
        errorMessage = $"can not verify Source account number";
        return false;
      }
      errorMessage = "Ok";
      return true;
    }
  }
}

