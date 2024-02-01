using System.Collections.Generic;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregationTemp.Aggregations.Dto;

public class TempCorporateCustomerAggregationDto
{
	public TblTempCorporateAccountAggregation TempAggregationInfo { get; set; }
	public List<string> RelatedAccounts { get; set; }
}

