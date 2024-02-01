using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblUserAccess
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool? IsCorporate { get; set; }
        public string Description { get; set; }
    }
}
