using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregation.Accounts;

public interface IAggregatedAccountRepository : IRepository<TblAggregatedAccount>
{
	List<TblAggregatedAccount> GetCorporateAggregationAccountByAggregationId(Guid corporateCustomerId);
	List<TblAggregatedAccount> GetCorporateAggregationAccountByAggregateId(Guid aggregatedId);
	TblAggregatedAccount GetCorporateAggregationAccountByAccountNumber(string accountNumber);
	TblAggregatedAccount GetCorporateAggregationAccountByAccountNumberAndCorporateCustomer(string accountNumber, Guid corporateCustomerId);
}
