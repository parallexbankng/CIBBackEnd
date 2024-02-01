using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblTempBankProfile
    {
        public Guid Id { get; set; }
        public Guid? BankProfileId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string UserRoles { get; set; }
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
        public long Sn { get; set; }
        public int? IsTreated { get; set; }
        public int? PreviousStatus { get; set; }
        public int? RegStage { get; set; }
    }
}
