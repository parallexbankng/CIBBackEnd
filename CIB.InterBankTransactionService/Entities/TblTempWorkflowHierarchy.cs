using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblTempWorkflowHierarchy
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? WorkflowHierarchyId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? WorkflowId { get; set; }
        public int? AuthorizationLevel { get; set; }
        public Guid? ApproverId { get; set; }
        public string ApproverName { get; set; }
        public decimal? AccountLimit { get; set; }
        public string RoleName { get; set; }
        public Guid? RoleId { get; set; }
        public string Reasons { get; set; }
        public string Action { get; set; }
        public int? PreviousStatus { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public int? IsTreated { get; set; }
        public int? Status { get; set; }
    }
}
