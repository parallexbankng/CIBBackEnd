using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Transaction.Dto.Interbank
{
  public class InterBankPostDto
  {
     public string accountToDebit { get; set; }
    public string userName { get; set; }
    public string channel { get; set; }
    public string transactionLocation { get; set; }
    public List<InterTransferDetail> interTransferDetails { get; set; }
  }

  public class InterTransferDetail
    {
      public string transactionReference {get;set;}
      public string beneficiaryAccountNumber { get; set; }
      public string beneficiaryAccountName { get; set; }
      public string transactionDate {get;set;}
      public decimal? amount { get; set; }
      public string beneficiaryKYC { get; set; }
      public string beneficiaryBVN { get; set; }
      public string beneficiaryBankCode { get; set; }
      public string beneficiaryBankName { get; set; }
      public string customerRemark { get; set; }
      public string nameEnquirySessionID { get; set; }
  }

  public class InterBankTransactionDto : BaseTransactioDto
    {
      public string SourceAccountNumber { get; set; }
      public string DestinationAccountNumber { get; set; }
      public string DestinationBankCode { get; set; }
      public string SourceBankName { get; set; }
      public string DestinationBankName { get; set; }
      public string SourceAccountName { get; set; }
      public string DestinationAccountName { get; set; }
      public string Amount { get; set; }
      public string Narration { get; set; }
      public string Otp { get; set; }
      public string WorkflowId { get; set; }
      public string TransactionType { get; set; }
    }

  public class InterBankTransaction : BaseTransactioDto
    {
      public string SourceAccountNumber { get; set; }
      public string DestinationAccountNumber { get; set; }
      public string DestinationBankCode { get; set; }
      public string SourceBankName { get; set; }
      public string DestinationBankName { get; set; }
      public string SourceAccountName { get; set; }
      public string DestinationAccountName { get; set; }
      public decimal Amount { get; set; }
      public string Narration { get; set; }
      public string Otp { get; set; }
      public Guid? WorkflowId { get; set; }
      public string TransactionType { get; set; }
      public string TransactionLocation {get;set;}
    }

}