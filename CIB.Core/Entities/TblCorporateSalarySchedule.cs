using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblCorporateSalarySchedule
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateApproved { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? WorkFlowId { get; set; }
        public string InitiatorUserName { get; set; }
        public Guid? ApproverId { get; set; }
        public string ApproverUserName { get; set; }
        public int? Status { get; set; }
        public byte? IsSalary { get; set; }
        public string AccountName { get; set; }
        public string Currency { get; set; }
        public string TransactionLocation { get; set; }
    }
}
