using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblPasswordHistory
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string CustomerProfileId { get; set; }
        public string Password { get; set; }
    }
}
