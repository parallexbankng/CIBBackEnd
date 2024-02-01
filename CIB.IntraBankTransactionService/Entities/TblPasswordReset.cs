using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblPasswordReset
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? AuthId { get; set; }
        public string ResetCode { get; set; }
        public DateTime? DateGenerated { get; set; }
        public DateTime? ResetDate { get; set; }
        public int? Status { get; set; }
    }
}
