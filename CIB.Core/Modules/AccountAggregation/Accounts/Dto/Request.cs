using System;
using System.Collections.Generic;
using CIB.Core.Common;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregation.Accounts.Dto;



public class AggregateCorporateCustomerPayload : BaseDto
{
	public string Id { get; set; }
	public string CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public List<string> AccountNumbers { get; set; }
}

public class AccountEnquire : BaseDto
{
	public Guid CorporateCustomerId { get; set; }
	public string AccountNumber { get; set; }
}
