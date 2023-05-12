using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Services.Api.Dto
{
  public class CustomerDataResponseDto
  {
    public string RequestId { get; set; }
    public string ResponseCode { get; set; }
    public string ResponseDescription { get; set; }
    public string CustomerId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string AccountType { get; set; }
    public string FreezeCode { get; set; }
    public string ProductCode { get; set; }
    public string Product { get; set; }
    public string AccountStatus { get; set; }
    public string CurrencyCode { get; set; }
    public string BranchCode { get; set; }
    public string Branch { get; set; }
    public string BookBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public string LienAmount { get; set; }
    public string UnclearedBalance { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string IsoCode { get; set; }
    public string RelationshipManagerId { get; set; }
    public string Bvn { get; set; }
    public string KycLevel { get; set; }
    public string Dob { get; set; }
    public string Effectiveavail { get; set; }
    public string Address { get; set; }
    public string FreezeReason { get; set; }
    public string AcctOffEmpId { get; set; }
    public string AcctOffEmpEmail { get; set; }
    public string AccOffPhone { get; set; }
    public string AccOfficerName { get; set; }
    public string ErrorDetail { get; set; }
    public string RiskRating { get; set; }
    public string DateAccountOpened { get; set; }
    public string token { get; set; }
  }
  public class StatementOfAccountResponseModel
  {
    public string respMsg { get; set; }
    public int respCode { get; set; }
    public List<AccountResponseData> data { get; set; }
  }
  public class AccountResponseData
  {
    public string referenceNo { get; set; }
    public string accountNumber { get; set; }
    public string transactionDate { get; set; }
    public decimal amount { get; set; }
    public string currency { get; set; }
    public decimal availableBalance { get; set; }
    public decimal bookBalance { get; set; }
    public string transactionType { get; set; }
    public string transactionStatus { get; set; }
    public string valueDate { get; set; }
    public string narration { get; set; }
    public string chargeRef { get; set; }
  }
  public class NameInquiryResponseModel
  {
    public string respMsg { get; set; }
    public int respCode { get; set; }
    public CustomerDataResponseDto data { get; set; }
  }
  public class AuthTokenResponse
  {
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
    public string Token { get; set; }
    public string Expiration { get; set; }
  }
  public class InterbankBalanceEnquiryResponseModel
  {
    public bool Success { get; set; }
    public InterbankBalanceEnquiryResponseData Data { get; set; }
    public String Message { get; set; }
  }
  public class InterbankBalanceEnquiryResponseData
  {
    public decimal bal { get; set; }
    public string cusName { get; set; }
    public string bvn { get; set; }
    public string customerCategory { get; set; }
    public string currency { get; set; }
  }
  public class InterbankNameEnquiryResponseModel
  {
    public bool success { get; set; }
    public InterbankNameEnquiryResponseData data { get; set; }
    public String message { get; set; }
  }
  public class InterbankNameEnquiryResponseData
  {
    public string responseCode { get; set; }
    public string acctName { get; set; }
    public string bvn { get; set; }
    public string kyc { get; set; }
    public string sessionsID { get; set; }
  }
  public class BankListResponseData
  {
    public string? ResponseCode{get;set;} 
    public string? ResponseMessage {get;set;}
    public List<BankDto> Banks { get; set; }
  }

  public class RelatedCustomerAccountDetail
  {
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? AccountType { get; set; }
    public string? FreezeCode { get; set; }
    public string? ProductCode { get; set; }
    public string? Product { get; set; }
    public string? AccountStatus { get; set; }
    public string? CurrencyCode { get; set; }
    public string? BranchCode { get; set; }
    public string? Branch { get; set; }
    public decimal? AvailableBalance { get; set; }
    public string? LienAmount { get; set; }
    public string? UnclearedBalance { get; set; }
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public string? RelationshipManagerId { get; set; }
    public string? ISOCODE { get; set; }
    public string? BookBalance { get; set; }
    public string? ACCT_CLS_FLG {get;set;}
  }
  public class RelatedCustomerAccountDetailsDto {
    public string Requestid { get; set; }
    public string RespondCode { get; set; }
    public string RespondMessage { get; set; }
    public List<RelatedCustomerAccountDetail>? Records { get; set; }
   }
  public class IntraBankTransferResponse
  {
    public string ResponseCode { get; set; }
    public string Status { get; set; }
    public string[] Errors {get;set;}
    public string ResponseDescription { get; set; }
    public string TransactionAmount { get; set; }
    public string TransactionAmountInWords { get; set; }
    public string TransactionDate { get; set; }
    public string TransactionReference { get; set; }
    public string AccountDebited { get; set; }
    public string AccountCredited { get; set; }
    public string SenderName { get; set; }
    public string BeneficiaryName { get; set; }
    public bool? HasFailed { get; set; }
  }
  public class InterbankNameEnquiryResponseDto {
    public string RequestId { get; set; }
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string BVN { get; set; }
    public string KYCLevel { get; set; }
  }

  public class AdUserInfo
  {
    public string ResponseCode { get; set; }
    public string ResponseDescription { get; set; }
    public ADUserData Data { get; set; }
  }

  public class ADUserData {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string Username { get; set; }
  }

  public class ADLoginResponseDto
  {
    public string ResponseMessage { get; set; }
    public bool IsAuthenticated { get; set; }
    public ADLoginData UserDetails { get; set; }
  }

  public class ADLoginData
  {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Surname { get; set; }
    public string MiddleName { get; set; }
    public string UserId { get; set; }
    public string EmployeeId { get; set; }
    public string Email { get; set; }
  }

  

  public class RequeryTransactionResponse
  {
    public string BeneficiaryAccountNumber {get;set;}
    public string TransactionReference {get;set;}
    public decimal  Amount {get;set;}
    public string ResponseCode {get;set;}
    public string  ResponseDescription {get;set;}
  }


}
