using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Modules.WorkflowHierarchy.Dto;

namespace CIB.Core.Modules.Workflow.Dto
{
    public class CreateWorkflowDto : BaseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int NoOfAuthorizers { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string? TransactionType { get; set; }
    }

    public class CreateWorkflow : BaseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string NoOfAuthorizers { get; set; }
        public string CorporateCustomerId { get; set; }
        public string ApprovalLimit { get; set; }
        public string TransactionType { get; set; }
    }

    public class CreateCorporateWorkflowDto : BaseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int NoOfAuthorizers { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string? TransactionType { get; set; }
        public List<CreateWorkflowHierarchyDto>WorkflowHierarchies { get; set; }
    }

    public class CreateCorporateWorkflow : BaseUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string NoOfAuthorizers { get; set; }
        public string CorporateCustomerId { get; set; }
        public string ApprovalLimit { get; set; }
        public string TransactionType { get; set; }
        public List<CreateWorkflowHierarchyDto>WorkflowHierarchies { get; set; }
    }

    public class UpdateWorkflowDto: BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int NoOfAuthorizers { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string? TransactionType { get; set; }
    }

    public class UpdateWorkflow :BaseUpdateDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string NoOfAuthorizers { get; set; }
        public string CorporateCustomerId { get; set; }
        public string ApprovalLimit { get; set; }
        public string TransactionType { get; set; }
    }
}