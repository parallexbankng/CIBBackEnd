using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Modules.Transaction.Dto.Interbank;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.Transaction.Dto;

namespace CIB.Core.Services.Api
{
  public interface IApiService
  {
    Task<CustomerDataResponseDto> GetCustomerDetailByAccountNumber(string accountNumber);
    Task<RelatedCustomerAccountDetailsDto> RelatedCustomerAccountDetails(string CustomerId);
    Task<CustomerDataResponseDto> CustomerNameInquiry(string accountNumbe);
    Task<IntraBankTransferResponse> IntraBankTransfer(IntraBankPostDto transaction);
    Task<BulkIntraBankTransactionResponse> IntraBankBulkTransfer(BulkIntrabankTransactionModel transaction);
    Task<IntraBankTransferResponse> InterBankTransfer(InterBankPostDto transaction);
    Task<CustomerDataResponseDto> QueryTransferTransaction(string accountNumber);
    Task<StatementOfAccountResponseDto> GenerateStatement(StatementOfAccountRequestDto accountRequestDto);
    Task<BankListResponseData> GetBanks();
    Task<AuthTokenResponse> GetAuthToken();
    Task<InterbankNameEnquiryResponseDto> BankNameInquire(string accountNumber, string bankCode);
    Task<AdUserInfo> ADBasicInfoInquire(string UserName);
    Task<ADLoginResponseDto> ADLogin(string Username, string Password);
  }
}