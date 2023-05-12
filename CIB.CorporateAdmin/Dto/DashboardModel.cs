using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Services.Api.Dto;

namespace CIB.CorporateAdmin.Dto
{
    public class DashboardModel
    {
        public List<RelatedCustomerAccountDetail> Accounts { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransaction { get; set; }
        public int SuccessfulTransactions { get; set; }
    }
}