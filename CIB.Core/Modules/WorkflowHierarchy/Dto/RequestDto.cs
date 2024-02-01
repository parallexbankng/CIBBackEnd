using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.WorkflowHierarchy.Dto
{
    public class CreateWorkflowHierarchyDto: BaseDto
    {
        public Guid WorkflowId { get; set; }
        public int AuthorizationLevel { get; set; }
        public Guid ApproverId { get; set; }
        public string ApproverName { get; set; }
        public string RoleName { get; set; }
        public Guid RoleId { get; set; }
    }

    public class CreateWorkflowHierarchy: BaseUpdateDto
    {
        public string WorkflowId { get; set; }
        public string AuthorizationLevel { get; set; }
        public string ApproverId { get; set; }
        public string ApproverName { get; set; }
        public string RoleName { get; set; }
        public string RoleId { get; set; }
    }

    public class UpdateWorkflowHierarchyDto
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public int AuthorizationLevel { get; set; }
        public Guid ApproverId { get; set; }
        public string ApproverName { get; set; }
        public decimal? AccountLimit { get; set; }
        public string RoleName { get; set; }
        public Guid RoleId { get; set; }
    }
}