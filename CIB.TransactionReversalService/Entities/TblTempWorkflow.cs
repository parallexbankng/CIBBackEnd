using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblTempWorkflow
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? WorkflowId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public DateTime? Date { get; set; }
        public int? NoOfAuthorizers { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TransactionType { get; set; }
        public string ReasonForDeclining { get; set; }
        public int? Status { get; set; }
        public int? PreviousStatus { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string Reasons { get; set; }
        public string Action { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public int? IsTreated { get; set; }
    }
}
