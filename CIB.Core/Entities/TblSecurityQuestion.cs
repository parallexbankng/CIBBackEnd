using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblSecurityQuestion
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public int? Type { get; set; }
    }
}
