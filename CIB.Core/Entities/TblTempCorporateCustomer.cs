using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblTempCorporateCustomer
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public long Sn { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string AuthorizationType { get; set; }
        public int? Status { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
        public string Reasons { get; set; }
        public string Action { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public int? IsTreated { get; set; }
        public int? PreviousStatus { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public Guid? CorporateRoleId { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal? IsApprovalByLimit { get; set; }
        public string UserName { get; set; }
        public int? RegStage { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public string Nationality { get; set; }
        public string CorporateEmail { get; set; }
    }
}
