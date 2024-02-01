using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Cheque.Dto
{
    public class TempResponseChequeBookDto
    {
        public Guid Id { get; set; }
        public string ApprovalUsername { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string PickupBranch { get; set; }
        public string NumberOfLeave { get; set; }
        public int? Status { get; set; }
        public DateTime? DateRequested { get; set; }
    }

    public class DuplicateStatus 
    {
        public string Message { get; set; }
        public bool IsDuplicate { get; set; }
    }
}