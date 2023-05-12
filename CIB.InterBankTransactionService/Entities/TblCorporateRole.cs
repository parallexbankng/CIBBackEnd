using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblCorporateRole
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public string RoleName { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public string ReasonsForDeclining { get; set; }
        public int? Status { get; set; }
        public Guid? CorporateCustomerId { get; set; }
    }
}
