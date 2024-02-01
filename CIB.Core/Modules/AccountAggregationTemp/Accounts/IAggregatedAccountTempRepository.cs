using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregationTemp.Accounts;

public interface ITempAggregatedAccountRepository : IRepository<TblTempAggregatedAccount>
{
	TblTempAggregatedAccount GetCorporateCustomerAggregationTempByAggregationCustomerID(Guid aggregationId);
	List<TblTempAggregatedAccount> GetAllCorporateAggregationAccountByAggregationId(Guid? aggregationId);
	void UpdateAggregatedAccount(TblTempAggregatedAccount request);
	void UpdateAggregatedAccountList(List<TblTempAggregatedAccount> request);

}
