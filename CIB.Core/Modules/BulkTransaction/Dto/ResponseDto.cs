using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.BulkTransaction.Dto
{
    public class VerifyBulkTransactionResponseDto
    {
        public string AccountName { get; set; }
        public string CreditAccount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Narration { get; set; }
        public string BankCode { get; set; }
        public string BankName{ get; set; }
        public string Error { get; set; }
       // public string Error { get; set; }
    }

    public class VerifyBulkTransactionResponse
    {
        public decimal Vat {get;set;}
        public decimal Fee {get;set;}
        public decimal TotalAmount { get; set; }
        public List<VerifyBulkTransactionResponseDto> Transaction { get; set; }
  }
}