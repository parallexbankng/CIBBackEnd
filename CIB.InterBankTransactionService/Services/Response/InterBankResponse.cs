namespace CIB.InterBankTransactionService.Services.Response;

public class TransferResponse
{
  public string? ResponseCode { get; set; }
  public string? ResponseDescription { get; set; }
  public string? TransactionAmount { get; set; }
  public string? TransactionAmountInWords { get; set; }
  public string? TransactionDate { get; set; }
  public string? TransactionReference { get; set; }
  public string? AccountDebited { get; set; }
  public string? AccountCredited { get; set; }
  public string? SenderName { get; set; }
  public string? BeneficiaryName { get; set; }
}
public class AuthTokenResponse {
  public string? ResponseCode { get; set; }
  public string? ResponseMessage { get; set; }
  public string? Token { get; set; }
  public string? Expiration { get; set; }
}

public class NameEnquiryResponse
{
  public string? RequestId { get; set; }
  public string? ResponseCode { get; set; }
  public string? ResponseMessage { get; set; }
  public string? AccountNumber { get; set; }
  public string? AccountName { get; set; }
  public string? BVN { get; set; }
  public string? KYCLevel { get; set; }
}

public class RequeryTransactionResponse 
{
  public string? ResponseCode { get; set; }
  public string? ResponseMessage { get; set; }
  public string? TransactionReference {get;set;}
}