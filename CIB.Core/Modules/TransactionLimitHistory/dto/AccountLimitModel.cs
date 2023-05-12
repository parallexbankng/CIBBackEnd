using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.TransactionLimitHistory.dto
{
    public class AccountLimitModel
    {
         public decimal? CorporateCustomerMaxLimit { get; set; }
        public decimal? CurrentUserMaxLimit { get; set; }
        public decimal? BulkTransMaxDailyLimit { get; set; }
        public decimal? SingleTransMaxDailyLimit { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string CustomerId { get; set; }
        public decimal? SingleTransTotalAmount { get; set; }
        public int? SingleTransTotalCount { get; set; }
        public decimal? BulkTransTotalAmount { get; set; }
        public int? BulkTransTotalCount { get; set; }
        public decimal? SingleTransAmountLeft { get; set; }
        public decimal? BulkTransAmountLeft { get; set; }
        public DateTime? Date { get; set; }
        public string Currency { get; set; }
    }
}