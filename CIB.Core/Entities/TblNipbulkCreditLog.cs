using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblNipbulkCreditLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? TranLogId { get; set; }
        public string CreditAccountNumber { get; set; }
        public Guid? BatchId { get; set; }
        public string CreditAccountName { get; set; }
        public string CreditBankCode { get; set; }
        public decimal? CreditAmount { get; set; }
        public int? CreditStatus { get; set; }
        public string BankVerificationNo { get; set; }
        public string NameEnquiryRef { get; set; }
        public string KycLevel { get; set; }
        public string ChannelCode { get; set; }
        public string Narration { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime? CreditDate { get; set; }
        public int? NameEnquiryStatus { get; set; }
        public string CreditBankName { get; set; }
        public string TransactionReference { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public int? TryCount { get; set; }
        public Guid? CreditReversalId { get; set; }
        public DateTime? InitiateDate { get; set; }
        public decimal? Fee { get; set; }
        public decimal? Vat { get; set; }
        public string? SessionId { get; set; }
        public string? TransactionResponseCode { get; set; }
        public string? TransactionResponseMessage { get; set; }
        
    }
}
