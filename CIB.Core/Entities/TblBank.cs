using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblBank
    {
        public Guid Id { get; set; }
        public int? Sn { get; set; }
        public string Name { get; set; }
        public string SortCode { get; set; }
        public string Branch { get; set; }
    }
}
