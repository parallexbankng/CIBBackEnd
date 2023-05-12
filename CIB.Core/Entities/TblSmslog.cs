using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblSmslog
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string RecieverPhone { get; set; }
        public string Message { get; set; }
        public int? Status { get; set; }
        public DateTime? Datelogged { get; set; }
        public DateTime? DateSent { get; set; }
    }
}
