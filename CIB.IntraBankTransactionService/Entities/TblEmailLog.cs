using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblEmailLog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string Senderemail { get; set; }
        public string Recieveremail { get; set; }
        public string Subject { get; set; }
        public string Messgebody { get; set; }
        public int? Status { get; set; }
        public string Copyemail { get; set; }
        public DateTime? DateLogged { get; set; }
        public DateTime? DateSent { get; set; }
    }
}
