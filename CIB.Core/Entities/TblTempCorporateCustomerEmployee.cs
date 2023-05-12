using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblTempCorporateCustomerEmployee
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? CorporateCustomerEmployeeId { get; set; }
        public Guid? InitiatorId { get; set; }
        public string InitiatorUserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StaffId { get; set; }
        public string Department { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public decimal? SalaryAmount { get; set; }
        public string GradeLevel { get; set; }
        public string Description { get; set; }
        public int? Status { get; set; }
        public DateTime? DateCreated { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? DateApproved { get; set; }
        public string Action { get; set; }
        public string Reasons { get; set; }
        public int? IsTreated { get; set; }
        public int? PreviousStatus { get; set; }
    }
}
