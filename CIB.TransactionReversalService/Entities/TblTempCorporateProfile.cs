using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblTempCorporateProfile
    {
        public Guid Id { get; set; }
        public Guid? CorporateProfileId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public long Sn { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public string Username { get; set; }
        public string Phone1 { get; set; }
        public string Email { get; set; }
        public string CorporateRole { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Status { get; set; }
        public string Reasons { get; set; }
        public string Action { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public int? IsTreated { get; set; }
        public int? RegStage { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public int? PreviousStatus { get; set; }
    }
}
