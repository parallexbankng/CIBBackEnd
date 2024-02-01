using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblIntrabankbeneficiary
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CustAuth { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public int? Status { get; set; }
        public DateTime? DateAdded { get; set; }
    }
}
