
using System;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TransactionLimitHistory
{
    public interface ITransactionHistoryRepository : IRepository<TblCorporateCustomerDailyTransLimitHistory>
    {
        TblCorporateCustomerDailyTransLimitHistory GetTransactionHistory(Guid CorporateCustomerId, DateTime date);
        void SetOrUpdateDailySingleTransLimitHistory(TblCorporateCustomer customer, decimal transactionAmount);
        void SetOrUpdateDailyBulkTransLimitHistory(TblCorporateCustomer customer, decimal transactionAmount);
    }
}