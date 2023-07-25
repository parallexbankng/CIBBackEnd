namespace CIB.InterBankTransactionService.Services.Request;

  public class PostInterBankTransaction
  {
    public string? accountToDebit { get; set; }
    public string? userName { get; set; }
    public string? channel { get; set; }
    public string? transactionLocation { get; set; }
    public List<InterTransferDetail>? interTransferDetails { get; set; }
  }
  public class InterTransferDetail
  {
    public string? transactionReference {get;set;}
    public string? beneficiaryAccountNumber { get; set; }
    public string? beneficiaryAccountName { get; set; }
    public string? transactionDate {get;set;}
    public decimal? amount { get; set; }
    public string? beneficiaryKYC { get; set; }
    public string? beneficiaryBVN { get; set; }
    public string? beneficiaryBankCode { get; set; }
    public string? beneficiaryBankName { get; set; }
    public string? customerRemark { get; set; }
    public string? nameEnquirySessionID { get; set; }
  }

  public class RequeryTransaction
  {
    public string? UserName { get; set; }
    public string? TransactionReference {get;set;}
    public string? BeneficiaryAccountNumber { get; set; }
    public string? BeneficiaryBankCode { get; set; }
    public string? AccountToDebit { get; set; }
    public decimal? Amount { get; set; }
  }

