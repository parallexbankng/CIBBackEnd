namespace CIB.TransactionReversalService.Services.Request;

public class PostIntraBankTransaction
{
  public string? AccountToDebit { get; set; }
  public string? UserName { get; set; }
  public string? Channel { get; set; }
  public string? TransactionLocation { get; set; }
  public List<IntraTransferDetail>? IntraTransferDetails { get; set; }
}
public class IntraTransferDetail
{
  public string? TransactionReference {get;set;}
  public string? TransactionDate { get; set; }
  public string? BeneficiaryAccountNumber { get; set; }
  public string? BeneficiaryAccountName { get; set; }
  public decimal? Amount { get; set; }
  public string? Narration { get; set; }
}
public class CustomerDataResponseDto
{
  public string? RequestId { get; set; }
  public string? ResponseCode { get; set; }
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
