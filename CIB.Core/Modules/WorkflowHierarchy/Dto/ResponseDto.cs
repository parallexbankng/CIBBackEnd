using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.WorkflowHierarchy.Dto
{
    public class WorkflowHierarchyResponseDto
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string WorkflowId { get; set; }
        public int AuthorizationLevel { get; set; }
        public string ApproverId { get; set; }
        public string ApproverName { get; set; }
        public decimal? AccountLimit { get; set; }
        public string RoleName { get; set; }
        public string RoleId { get; set; }
    }
}