using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblTokenBlackCorp
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public string TokenCode { get; set; }
        public Guid? CustAutId { get; set; }
        public DateTime? DateGenerated { get; set; }
        public int? IsBlack { get; set; }
    }
}
