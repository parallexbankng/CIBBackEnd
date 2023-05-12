﻿using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblPendingCreditLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? TranLogId { get; set; }
        public string CreditAccountNumber { get; set; }
        public string CreditAccountName { get; set; }
        public string CreditBankCode { get; set; }
        public decimal? CreditAmount { get; set; }
        public int? CreditStatus { get; set; }
        public DateTime? CreditDate { get; set; }
        public string BankVerificationNo { get; set; }
        public string NameEnquiryRef { get; set; }
        public string KycLevel { get; set; }
        public string ChannelCode { get; set; }
        public string Narration { get; set; }
        public string CreditBankName { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string TransactionReference { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public int? TryCount { get; set; }
    }
}
