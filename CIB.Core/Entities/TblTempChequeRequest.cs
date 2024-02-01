﻿using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblTempChequeRequest
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ApprovedId { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string InitiatorUsername { get; set; }
        public string ApprovalUsername { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string PickupBranch { get; set; }
        public string NumberOfLeave { get; set; }
        public string ReasonForDeclining { get; set; }
        public int? Status { get; set; }
        public int? PreviousStatus { get; set; }
        public string Reasons { get; set; }
        public string Action { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? ActionResponseDate { get; set; }
        public int? IsTreated { get; set; }
        public Guid? ChequeRequetId { get; set; }
        public int? BranchId { get; set; }
        public string CorporateCustomer { get; set; }
    }
}
