namespace CIB.IntraBankTransactionService.Services.Request;

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

