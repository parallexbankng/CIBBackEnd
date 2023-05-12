
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Transaction.Dto;

namespace CIB.Core.Modules.Transaction
{
    public interface ITransactionRepository : IRepository<TblTransaction>
    {
        List<TblTransaction> GetCorporateTransactions(Guid CorporateCustomerId);
        List<TblTransaction> GetAllCorporateTransactions();
        TransactionReportDto GetTransactionReportDetail();
        IEnumerable<TransactionReportDto> Search(Guid? corporateCustomerId, string transactionRef, DateTime dateFrom, DateTime dateTo,bool IsBulk); 
        IEnumerable<TblTransaction> GetCorporateTransactionReport(Guid CorporateCustomerId);
    }

}