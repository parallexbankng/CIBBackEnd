using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Cheque.Dto
{
    public class ResponseChequeBookDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string PickupBranch { get; set; }
        public string NumberOfLeave { get; set; }
        public int? Status { get; set; }
        public DateTime? ActionResponseDate { get; set; }
    }
}