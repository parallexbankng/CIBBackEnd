using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblWorkflow
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public DateTime? Date { get; set; }
        public int? NoOfAuthorizers { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TransactionType { get; set; }
        public string ReasonForDeclining { get; set; }
        public int? Status { get; set; }
        public decimal? ApprovalLimit { get; set; }
    }
}
