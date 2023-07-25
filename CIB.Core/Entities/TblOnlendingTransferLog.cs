using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblOnlendingTransferLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid BatchId { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? NumberOfCredit { get; set; }
        public int? TotalCredit { get; set; }
        public int? TotalFailed { get; set; }
        public int? Status { get; set; }
        public DateTime? DateInitiated { get; set; }
        public string InitiatorUserName { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? WorkflowId { get; set; }
        public int? ApprovalStage { get; set; }
        public string TransactionLocation { get; set; }
        public string DebitAccountNumber { get; set; }
        public string DebitAccountName { get; set; }
        public int? ApprovalCount { get; set; }
        public string TransferType { get; set; }
        public string Currency { get; set; }
        public string TransactionReference { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string ErrorDetail { get; set; }
        public DateTime? DateProccessed { get; set; }
        public int? ApprovalStatus { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public int? TransactionStatus { get; set; }
        public string PostingType { get; set; }
        public int? ValidCount { get; set; }
        public int? InValidCount { get; set; }
        public decimal? TotalValidAmount { get; set; }
        public string SessionId { get; set; }
        public string OperatingAccountNumber { get; set; }
        public string OperatingAccountName { get; set; }
    }
}
