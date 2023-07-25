using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblNipbulkTransferLog
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? BatchId { get; set; }
        public string DebitAccountNumber { get; set; }
        public string DebitAccountName { get; set; }
        public decimal? DebitAmount { get; set; }
        public DateTime? DateInitiated { get; set; }
        public DateTime? DateProccessed { get; set; }
        public int? ApprovalStatus { get; set; }
        public string PostingType { get; set; }
        public int? Status { get; set; }
        public int? AboutTo { get; set; }
        public int? NoOfCredits { get; set; }
        public int? TransactionStatus { get; set; }
        public string TransferType { get; set; }
        public int? ApprovalStage { get; set; }
        public string Currency { get; set; }
        public string Narration { get; set; }
        public string OriginatorBvn { get; set; }
        public int? ApprovalCount { get; set; }
        public string Comment { get; set; }
        public int? TryCount { get; set; }
        public string BulkFileName { get; set; }
        public string BulkFilePath { get; set; }
        public string TransactionLocation { get; set; }
        public string DebitMode { get; set; }
        public string SuspenseAccountNumber { get; set; }
        public string SuspenseAccountName { get; set; }
        public string InitiatorUserName { get; set; }
        public string TransactionReference { get; set; }
        public Guid? WorkflowId { get; set; }
        public int? TotalCredits { get; set; }
        public int? IntraBankStatus { get; set; }
        public int? InterBankStatus { get; set; }
        public string IntreBankSuspenseAccountNumber { get; set; }
        public string IntreBankSuspenseAccountName { get; set; }
        public int? InterBankTryCount { get; set; }
        public int? InterBankTotalCredits { get; set; }
         public string? SessionId { get; set; }
    }
}
