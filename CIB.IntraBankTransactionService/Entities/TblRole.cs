using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblRole
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public string RoleName { get; set; }
        public int? Grade { get; set; }
        public string ReasonsForDeclining { get; set; }
        public int? Status { get; set; }
    }
}
