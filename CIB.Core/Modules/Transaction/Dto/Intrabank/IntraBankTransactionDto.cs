using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Transaction.Dto.Intrabank
{
    public class IntraBankTransactionDto : BaseTransactioDto
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
        public string TransactionLocation { get; set; }
    }

    public class IntraBankTransaction : BaseTransactioDto
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
        public Guid WorkflowId { get; set; }
        public string TransactionType { get; set; }
        public string TransactionLocation { get; set; }
    }
    public class ApproveTransactionDto : BaseTransactioDto
    {
        public string AuthorizerId { get; set; }
        public string TranLogId { get; set; }
        public string Comment { get; set; }
        public string Otp { get; set; }
    }

     public class DeclineTransactionDto : BaseTransactioDto
    {
        public string TranLogId { get; set; }
        public string Comment { get; set; }
        public string Otp { get; set; }
    }

    public class IntraBankPostDto
    {
        public string AccountToDebit { get; set; }
        public string UserName { get; set; }
        public string Channel { get; set; }
        public string TransactionLocation { get; set; }
        public List<IntraTransferDetail> IntraTransferDetails { get; set; }
    }

    public class IntraTransferDetail
    {
        public string TransactionReference {get;set;}
        public string TransactionDate { get; set; }
        public string BeneficiaryAccountNumber { get; set; }
        public string BeneficiaryAccountName { get; set; }
        public decimal? Amount { get; set; }
        public string Narration { get; set; }
    }
}