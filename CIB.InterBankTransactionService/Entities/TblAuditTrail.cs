using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblAuditTrail
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public DateTime? TimeStamp { get; set; }
        public Guid? UserId { get; set; }
        public string NewFieldValue { get; set; }
        public string PreviousFieldValue { get; set; }
        public string ActionCarriedOut { get; set; }
        public string Ipaddress { get; set; }
        public string TransactionId { get; set; }
        public string ClientStaffIpaddress { get; set; }
        public string Macaddress { get; set; }
        public string HostName { get; set; }
        public string Description { get; set; }
    }
}
