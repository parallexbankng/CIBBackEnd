using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblPendingTranLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? InitiatorId { get; set; }
        public string DebitAccountNumber { get; set; }
        public string DebitAccountName { get; set; }
        public decimal? DebitAmount { get; set; }
        public DateTime? DateInitiated { get; set; }
        public int? ApprovalStatus { get; set; }
        public string PostingType { get; set; }
        public int? Status { get; set; }
        public int? NoOfCredits { get; set; }
        public int? TransactionStatus { get; set; }
        public string TransferType { get; set; }
        public int? ApprovalStage { get; set; }
        public string Currency { get; set; }
        public string Narration { get; set; }
        public string OriginatorBvn { get; set; }
        public int? ApprovalCount { get; set; }
        public string Comment { get; set; }
        public DateTime? DateApproved { get; set; }
        public Guid? ApprovedBy { get; set; }
        public Guid? WorkflowId { get; set; }
        public string TransactionLocation { get; set; }
        public string TransactionReference { get; set; }
        public Guid? BatchId { get; set; }
        public decimal? Vat { get; set; }
        public decimal? Fee { get; set; }
    }
}
