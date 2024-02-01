using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregationTemp.Accounts.Dto;

public class TempCorporateCustomerAggregationDto
{
	public TblTempAggregatedAccount AggregationInfo { get; set; }
	public List<string> RelatedAccounts { get; set; }
}

public class TempCorporate
{
	public TblTempAggregatedAccount AggregationInfo { get; set; }
	public List<string> RelatedAccounts { get; set; }
}

public class TempAggregateCorporateCustomerPayload : BaseDto
{
	public string Id { get; set; }
	public string CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public List<string> AccountNumbers { get; set; }
}
public class TempAggregateCorporateCustomer : BaseDto
{
	public string Id { get; set; }
	public string CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public List<string> AccountNumbers { get; set; }
}

public class TempAggregateCorporateCustomerModel : BaseDto
{
	public Guid? Id { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public List<string> AccountNumbers { get; set; }
}
