using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.TransactionReversalService.Modules.Common
{
    public class InterbankReversalDto
    {
        public decimal? Amount {get;set;}
        public string Narration {get;set;}
        public string? TranType {get;set;}
    }


}