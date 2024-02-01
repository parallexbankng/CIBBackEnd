using System;
using System.Collections.Generic;
using CIB.Core.Common;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;

namespace CIB.Core.Modules.AccountAggregation.Aggregations.Dto;

public class CreateAggregateCorporateCustomerDto : BaseDto
{
	public string? CorporateCustomerId { get; set; }
	public string? CorporateCustomer { get; set; }
	public string? AccountNumber { get; set; }
	public string? AccountName { get; set; }
	public string? CustomerId { get; set; }
}

public class CreateAggregateCorporateCustomerModel : BaseDto
{
	public Guid? CorporateCustomerId { get; set; }
	public string? CustomerId { get; set; }
	public string? DefaultAccountNumber { get; set; }
	public string? DefaultAccountName { get; set; }
	public List<AggregatedAccountResponseDto>? AccountNumbers { get; set; }
}


public class UpdatwAggregateCorporateCustomerModel : BaseUpdateDto
{
	public Guid Id { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public string? CustomerId { get; set; }
	public string? DefaultAccountNumber { get; set; }
	public string? DefaultAccountName { get; set; }
	public List<AggregatedAccountResponseDto>? AccountNumbers { get; set; }
}