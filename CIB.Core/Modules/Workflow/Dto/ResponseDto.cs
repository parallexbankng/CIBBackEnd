using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Workflow.Dto
{
    public class WorkFlowResponseDto
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public DateTime Date { get; set; }
        public int NoOfAuthorizers { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TransactionType { get; set; }
        public string ReasonForDeclining { get; set; }
        public int? Status { get; set; }
        public decimal? ApprovalLimit { get; set; }
    }
}