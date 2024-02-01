using System.Collections.Generic;

namespace CIB.Core.Services.OnlendingApi.Dto
{
  public class OnlendingAuthTokenResponse
  {
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
    public string Token { get; set; }
    public string Expiration { get; set; }
  }

  public class OnlendingResponseData
  {
    public string? Message { get; set; }
    public string? CanDrawDesiredAmount { get; set; }
    public string? NewDrawAmount { get; set; }
    public string? ValidationID { get; set; }
    public string? ExpectedBalance { get; set; }
    public decimal? Interest { get; set; }
    public string? AccountNumber { get; set; }
    public string? ExtendedDate { get; set; }
    public string? CifId { get; set; }
    public string? SchemeCode { get; set; }
    public string? AcctCurr { get; set; }
    public ErrorDetail? ErrorDetail { get; set; }
  }

  public class OnleandBvnValidationResponse
  {
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
    public string Bvn { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string DateOfBirth { get; set; }
    public string PhoneNumber1 { get; set; }
    public string RegistrationDate { get; set; }
    public string EnrollmentBank { get; set; }
    public string EnrollmentBranch { get; set; }
    public string Email { get; set; }
    public string Gender { get; set; }
    public string PhoneNumber2 { get; set; }
    public string LevelOfAccount { get; set; }
    public string LgaOfOrigin { get; set; }
    public string LgaOfResidence { get; set; }
    public string MaritalStatus { get; set; }
    public string Nin { get; set; }
    public string nameOnCard { get; set; }
    public string Nationality { get; set; }
    public string ResidentialAddress { get; set; }
    public string StateOfOrigin { get; set; }
    public string StateOfResidence { get; set; }
    public string Title { get; set; }
    public string WatchListed { get; set; }
    public string Base64Image { get; set; }
  }

  public class OnleandIdIssueValidationResponse
  {
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
  }

  public class OnlendingAccountOpeningResponse
  {
    public string ResponseCode { get; set; }
    public string ResponseDescription { get; set; }
    public bool? IsSuccessful { get; set; }
    public OnlendingResponseData? ResponseData { get; set; }
    public string[]? ErrorDetail { get; set; }
    public string? Message { get; set; }
    public List<ErrorObject>? Errors { get; set; }
  }

  public class OnlendingMerchantBeneficiaryResponse
  {
    public string? ResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
    public bool? IsSuccessful { get; set; }
    public List<MerchantBeneficiaryResponse>? ResponseData { get; set; }
    public string[]? ErrorDetail { get; set; }
    public string? Message { get; set; }
    public List<ErrorObject>? Errors { get; set; }
  }

  public class OnlendingDateExtendsionResponse
  {
    public string? ResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
    public bool? IsSuccessful { get; set; }
    public OnlendingResponseData? ResponseData { get; set; }
    public string[]? ErrorDetail { get; set; }
    public string? Message { get; set; }
    public List<ErrorObject>? Errors { get; set; }
  }


  public class MerchantBeneficiaryResponse
  {
    public int Id { get; set; }
    public string? MerchantAccountNumber { get; set; }
    public string? MerchantOperatingAccountNumber { get; set; }
    public string? BeneficiaryAccountNumber { get; set; }
    public string? RepaymentStatus { get; set; }
    public string? Limit { get; set; }
    public string? DrawingPower { get; set; }
    public string? Startdate { get; set; }
    public string? EndDate { get; set; }
    public string? ExtensionDate { get; set; }
    public string? InterestOnPOF { get; set; }
    public string? DateCreated { get; set; }
    public decimal? AmountDisbursted { get; set; }
    public decimal? AmountLiquidated { get; set; }
  }

  public class OnlendingLiquidationResponse
  {
    public string? ResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
    public bool? IsSuccessful { get; set; }
    public List<MerchantBeneficiaryResponse>? ResponseData { get; set; }
    public string[]? ErrorDetail { get; set; }
    public string? Message { get; set; }
    public List<ErrorObject>? Errors { get; set; }
  }



  //{"responseCode":"00","responseDescription":"SUCCESS","responseData":{"accountNumber":"2000213216","cif":"R000171218","acctCurr":"","errorDetail":null},"errorDetail":[]}

  public class ErrorDetail
  {
    public string ErrorCode { get; set; }
    public string ErrorDesc { get; set; }
    public string ErrorSource { get; set; }
    public string ErrorType { get; set; }
  }




  public class OnleandingResponse
  {
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public string? IsSuccessful { get; set; }
    public OnlendingResponseData? ResponseData { get; set; }
    public string? ErrorDetail { get; set; }
    public string? Message { get; set; }
    public List<ErrorObject>? Errors { get; set; }
  }


  public class BeneficiaryAdditionInfoRespons
  {
    public List<StateInfoDto>? State { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
  }

  public class StateInfoDto
  {
    public string StateCode { get; set; }
    public string StateName { get; set; }
    public List<DataInfo> Lga { get; set; }
  };

  public class DataInfo
  {
    public string LgaCode { get; set; }
    public string Lga { get; set; }

  }

  public class ErrorObject
  {
    public string Field { get; set; }
    public string Message { get; set; }

  }

  public class BeneficiaryStateResponse
  {
    public string LgaCode { get; set; }
    public string Lga { get; set; }
    public string StateCode { get; set; }
    public string State { get; set; }
  }

  public class IntrestCalculationResponse
  {
    public string AccountNumber { get; set; }
    public decimal? Interest { get; set; }
    public decimal? Amount { get; set; }
  }


}