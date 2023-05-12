using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblCorporateCustomer
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string CustomerId { get; set; }
        public Guid? Sector { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string Branch { get; set; }
        public int? ApprovalRequired { get; set; }
        public string AuthorizationType { get; set; }
        public DateTime? DateAdded { get; set; }
        public string AddedBy { get; set; }
        public int? ApprovalStatus { get; set; }
        public int? IsApprovalByLimit { get; set; }
        public int? NumberOfApproval { get; set; }
        public int? Status { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public string ReasonForDeclining { get; set; }
        public string ReasonForDeactivation { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public string CorporateEmail { get; set; }
    }
}
