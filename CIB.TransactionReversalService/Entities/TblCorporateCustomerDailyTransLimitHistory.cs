using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblCorporateCustomerDailyTransLimitHistory
    {
        public long Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string CustomerId { get; set; }
        public decimal? SingleTransTotalAmount { get; set; }
        public int? SingleTransTotalCount { get; set; }
        public decimal? BulkTransTotalAmount { get; set; }
        public int? BulkTransTotalCount { get; set; }
        public decimal? SingleTransAmountLeft { get; set; }
        public decimal? BulkTransAmountLeft { get; set; }
        public DateTime? Date { get; set; }
    }
}
