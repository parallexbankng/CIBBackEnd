using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;

namespace CIB.Core.Modules.AccountAggregation.Aggregations.Dto;

public partial class CorporateAccountAggregationResponse
{
	public Guid Id { get; set; }
	public long Sn { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public string CompanyName { get; set; }
	public string CorporateEmail { get; set; }
	public int? Status { get; set; }
	public DateTime? DateCreated { get; set; }
}
public class CorporateCustomerAggregationWithAccountNumberResponse
{
	public TblCorporateAccountAggregation AggregationInfo { get; set; }
	public List<AggregatedAccountsResponseDto> RelatedAccounts { get; set; }
}

public partial class CorporateCustomerAccountInquireResponse
{
	public Guid? CorporateCustomerId { get; set; }
	public string? DefaultAccountNumber { get; set; }
	public string? DefaultAccountName { get; set; }
	public string? CustomerId { get; set; }
	public List<AggregatedAccountResponseDto> RelatedAccounts { get; set; }
}
public class CorporateCustomerAggregationResponse
{
	public string ResponseMessage { get; set; }
	public string ResponseCode { get; set; }
	public List<TblAggregatedAccount> RelatedAccounts { get; set; }
}