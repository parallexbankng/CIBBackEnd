using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.AccountAggregation.Aggregations;

public interface ICorporateAggregationRepository : IRepository<TblCorporateAccountAggregation>
{

	TblCorporateAccountAggregation GetCorporateCustomerAggregationByID(Guid id, Guid? corporateCustomerId);

	CorporateUserStatus CheckDuplicate(TblCorporateAccountAggregation profile, bool IsUpdate = false);
	void UpdateAccountAggregation(TblCorporateAccountAggregation request);
	List<TblCorporateAccountAggregation> GetCorporateCustomerAggregations(Guid? corporateCustomerId);
	AggregationResponses GetAggregationByAggregationCustomerId(Guid? aggregationCustomerId);
	TblCorporateAccountAggregation GetCorporateAggregationByAggregationCustomerId(Guid? aggregationCustomerId);
	List<TblCorporateAccountAggregation> GetCorporateCustomerAggregationByAggregationCustomerId(string aggregationCustomerId);
	ItemStatus CheckDuplicateAggregate(TblCorporateAccountAggregation profile, bool reactivate = false);
	List<TblCorporateAccountAggregation> AdminGetCorporateCustomerAggregations(Guid? corporateCustomerId);

}
