using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.AuditTrial.Dto
{
    public class AuditTrialDto : BaseDto
    {
        public string Username { get; set; }
        public DateTime? TimeStamp { get; set; }
        public Guid? UserId { get; set; }
        public string NewFieldValue { get; set; }
        public string PreviousFieldValue { get; set; }
        public string ActionCarriedOut { get; set; }
        public string Ipaddress { get; set; }
        public string TransactionId { get; set; }
        public string Description { get; set; }
    }
     public class AuditTrialSearchDto : BaseDto
    {
        public string Username { get; set; }
        public string TimeStamp { get; set; }
        public string Action {get;set;}
        public string UserId { get; set; }
        public string DateFrom {get;set;}
        public string DateTo {get;set;}
    }
}