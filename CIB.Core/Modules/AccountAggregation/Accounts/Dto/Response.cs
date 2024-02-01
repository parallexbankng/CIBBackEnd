using System;
using System.Collections.Generic;
using CIB.Core.Entities;
using Microsoft.AspNetCore.SignalR;

namespace CIB.Core.Modules.AccountAggregation.Accounts.Dto;

public class AggregatedAccountsResponseDto
{
	public Guid Id { get; set; }
	public string AccountNumber { get; set; }
	public string AccountName { get; set; }
}
public class AggregatedAccountResponseDto
{
	public string? AccountNumber { get; set; }
	public string? AccountName { get; set; }
	public decimal? AvailableBalance { get; set; }
}

//public class AggregationResponses
//{
//	public TblTempCorporateAccountAggregation TempAggregatedAccount { get; set; }
//	public List<TblTempAggregatedAccount> AggregatedAccounts { get; set; }
//}

public class TempAggregationResponses
{
	public Guid Id { get; set; }
	public long Sn { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public Guid? AccountAggregationId { get; set; }
	public Guid? InitiatorId { get; set; }
	public string? DefaultAccountNumber { get; set; }
	public string? DefaultAccountName { get; set; }
	public string? CustomerId { get; set; }
	public string? InitiatorUserName { get; set; }
	public string? Reasons { get; set; }
	public string? Action { get; set; }
	public int? Status { get; set; }
	public int? IsTreated { get; set; }
	public int? PreviousStatus { get; set; }
	public DateTime? DateInitiated { get; set; }
	public DateTime? ActionResponseDate { get; set; }
	public List<TblTempAggregatedAccount> AccountNumbers { get; set; }
}

public class AggregationResponses
{
	public Guid Id { get; set; }
	public long Sn { get; set; }
	public Guid? CorporateCustomerId { get; set; }
	public string? DefaultAccountNumber { get; set; }
	public string? DefaultAccountName { get; set; }
	public string? CustomerId { get; set; }
	public int? Status { get; set; }
	public DateTime? DateCreated { get; set; }
	public List<TblAggregatedAccount> AccountNumbers { get; set; }
}



//public class PendingAggregationResponses
//{
//	public TblTempCorporateAccountAggregation TempAggregatedAccount { get; set; }
//	public List<AggregatedAccountResponseDto> AccountNumbers { get; set; }
//}

//public class PendingAggregationResponses
//{
//	public TblTempCorporateAccountAggregation TempAggregatedAccount { get; set; }
//	public List<AggregatedAccountResponseDto> AccountNumbers { get; set; }
//}

//public class PendingAggregationResponses
//{
//	public TblTempCorporateAccountAggregation TempAggregatedAccount { get; set; }
//	public List<AggregatedAccountResponseDto> AccountNumbers { get; set; }
//}

//{
//"CustomerId": "23c3eac6-80e1-45fe-904d-c47e1c6da630", 
//"DefaultAccountNumber": "2030080271",
//"DefaultAccountName": "CHRIST EMBASSY CONVENTIONS & CONFERENCES CORPORATE 3", 
// "CustomerId": "C000114041",
// "AccountNumbers": [{ "AccountNumber": "2030080271","AccountName": "CHRIST EMBASSY CONVENTIONS & CONFERENCES CORPORATE 3"}]}