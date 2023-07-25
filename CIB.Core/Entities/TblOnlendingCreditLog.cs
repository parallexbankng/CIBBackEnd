using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblOnlendingCreditLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? BeneficiaryId { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public decimal? FundAmount { get; set; }
		    public decimal? FundAmountInterest { get; set; }
		    public int? Status { get; set; }
		    public int? CreditStatus { get; set; }
		    public DateTime? DateCredited { get; set; }
		    public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? NumberOfDays { get; set; }
        public DateTime? ExtensionDate { get; set; }
		    public decimal? ExtensionInterest { get; set; }
		    public int? NumberOfDayExtension { get; set; }
        public DateTime? DateInitiated { get; set; }
        public Guid? BatchId { get; set; }
        public Guid? TranLogId { get; set; }
        public string BvnResponse { get; set; }
        public string BvnResponseCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
		    public string DisbursmentResponseCode { get; set; }
		    public string DisbursmentResponseMessage { get; set; }
		    public int? VerificationStatus { get; set; }
        public DateTime? RepaymentDate { get; set; }
        public DateTime? DateCreated { get; set; }
        public string Narration { get; set; }
        public string Error { get; set; }
        public string AccountNumber { get; set; }
        public string TransactionReference { get; set; }
        public string SessionId { get; set; }
    }
}
