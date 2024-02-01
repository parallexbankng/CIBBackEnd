using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.AccountAggregationTemp.Aggregations;
public interface ITempCorporateAggregationRepository : IRepository<TblTempCorporateAccountAggregation>
{
	TblTempCorporateAccountAggregation GetCorporateCustomerAggregations(Guid Id);
	List<TblTempCorporateAccountAggregation> GetPendingCorporateCustomerAggregations(Guid? corporateCustomerId);
	List<TblTempCorporateAccountAggregation> GetCorporateCustomerAggregationByAggregationCustomerId(string aggregationCustomerId);
	TblTempCorporateAccountAggregation GetCorporateCustomerAggregation(Guid aggregationId, Guid? corporateCustomerId);
	TblTempCorporateAccountAggregation GetCorporateCustomerAggregationByID(Guid Id, Guid? corporateCustomerId);
	List<AggregationResponses> GetPendingCorporateCustomerAggregationWithAccounts(Guid? corporateCustomerId);
	TempAggregationResponses GetCorporateAggregationAccountByAggregationId(Guid aggregationCustomerId);
	void UpdateAccountAggregation(TblTempCorporateAccountAggregation request);
	CorporateUserStatus CheckDuplicate(TblTempCorporateAccountAggregation profile, bool IsUpdate = false);
}
