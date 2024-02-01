using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblSpecialFeature
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public int? Status { get; set; }
        public string Reasons { get; set; }
        public string Feature { get; set; }
        public string Action { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public int? IsTreated { get; set; }
        public int? PreviousStatus { get; set; }
    }
}