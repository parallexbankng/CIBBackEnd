using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblWorkflowHierarchy
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? WorkflowId { get; set; }
        public int? AuthorizationLevel { get; set; }
        public Guid? ApproverId { get; set; }
        public string ApproverName { get; set; }
        public decimal? AccountLimit { get; set; }
        public string RoleName { get; set; }
        public Guid? RoleId { get; set; }
    }
}
