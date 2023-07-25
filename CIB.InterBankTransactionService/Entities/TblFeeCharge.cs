using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblFeeCharge
    {
        public long Id { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal? FeeAmount { get; set; }
        public decimal? Vat { get; set; }
    }
}
