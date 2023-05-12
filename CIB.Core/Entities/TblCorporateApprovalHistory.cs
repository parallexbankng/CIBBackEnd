using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblCorporateApprovalHistory
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? LogId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public Guid? UserId { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int? ApprovalLevel { get; set; }
        public string ApproverName { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public int? ToApproved { get; set; }
    }
}
