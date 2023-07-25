using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblOnlendingExtensionHistory
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? InitiatorId { get; set; }
        public string InitiatorUserName { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? BatchId { get; set; }
        public string ExtensionDuration { get; set; }
        public decimal? Intrest { get; set; }
        public DateTime? PreviouseRepaymentDate { get; set; }
        public DateTime? NewRepaymentDate { get; set; }
        public Guid? WorkFlowId { get; set; }
        public Guid? OnlendingCreditLogId { get; set; }
    }
}
