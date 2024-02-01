using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;

namespace CIB.Core.Modules.AccountAggregationTemp.Aggregations.Dto;

public class TempCorporateCustomerAggregationWithAccountNumberResponseDto
{
	public TblTempCorporateAccountAggregation TempAggregationInfo { get; set; }
	public List<AggregatedAccountsResponseDto> RelatedAccounts { get; set; }
}

public class TempCorporateAccountAggregationResponse
{
	public Guid Id { get; set; }
	public long Sn { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public string AggregateCustomerId { get; set; }
	public string DefaultAccountNumber { get; set; }
	public string DefaultAccountName { get; set; }
	public string CompanyName { get; set; }
	public string CorporateEmail { get; set; }
	public string Action { get; set; }
	public int? Status { get; set; }
	public string InitiatorUserName { get; set; }
	public DateTime? DateInitiated { get; set; }
	public DateTime? ActionResponseDate { get; set; }
}