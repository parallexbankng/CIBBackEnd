using System;
using System.Collections.Generic;


namespace CIB.Core.Modules.CorporateCustomer.Dto
{
	public class CorporateCustomerResponseDto
	{
		public Guid Id { get; set; }
		public string CompanyName { get; set; }
		public string CorporateEmail { get; set; }
		public string CompanyAddress { get; set; }
		public string Phone1 { get; set; }
		public string Phone2 { get; set; }
		public string Email1 { get; set; }
		public string Email2 { get; set; }
		public string CustomerId { get; set; }
		public Guid? Sector { get; set; }
		public string DefaultAccountNumber { get; set; }
		public string DefaultAccountName { get; set; }
		public string Branch { get; set; }
		public int? ApprovalRequired { get; set; }
		public string AuthorizationType { get; set; }
		public DateTime? DateAdded { get; set; }
		public string AddedBy { get; set; }
		public int? ApprovalStatus { get; set; }
		public int? IsApprovalByLimit { get; set; }
		public int? NumberOfApproval { get; set; }
		public int? Status { get; set; }
		public decimal? MinAccountLimit { get; set; }
		public decimal? MaxAccountLimit { get; set; }
		public decimal? BulkTransDailyLimit { get; set; }
		public decimal? SingleTransDailyLimit { get; set; }
		public string ReasonForDeclining { get; set; }
		public string ReasonForDeactivation { get; set; }
		public string CorporateShortName { get; set; }
		public decimal? AuthenticationLimit { get; set; }
	}
	public class AccountLimitResponse
	{
		public bool IsApprovalByLimit { get; set; }
		public Guid CorporateCustomerId { get; set; }
		public decimal? MinAccountLimit { get; set; }
		public decimal? MaxAccountLimit { get; set; }
		public decimal? BulkTransDailyLimit { get; set; }
		public decimal? SingleTransDailyLimit { get; set; }
		public string CorporateShortName { get; set; }
		public decimal? AuthenticationLimit { get; set; }
	}

	public class StatementOfAccountResponseDto
	{
		public string ResponseCode { get; set; }
		public string ResponseDescription { get; set; }
		public string OpeningBal { get; set; }
		public string ClosingBal { get; set; }
		public string TotalCredit { get; set; }
		public string TotalDebit { get; set; }
		public string AccountType { get; set; }
		public string AccountNumber { get; set; }
		public string Period { get; set; }
		public string StatementDate { get; set; }
		public string StatementPath { get; set; }
		// public string Attachement { get; set; }
		public string AttachementName { get; set; }
		public string? EffectiveBal { get; set; }
		public string? AvailableBal { get; set; }
		public string? Branch { get; set; }
		public string? Address { get; set; }
		public string DateRange { get; set; }
		public string CustomerName { get; set; }
		public List<Statement> Statement { get; set; }
	}

	public class Statement
	{
		public string TRANSDATE { get; set; }
		public string VALUEDATE { get; set; }
		public string DEBIT { get; set; }
		public string CREDIT { get; set; }
		public string BALANCE { get; set; }
		public string REMARKS { get; set; }
	}

	public class ChangeSignatoryDto
	{
		public Guid Id { get; set; }
		public string CompanyName { get; set; }
		public string CorporateShortName { get; set; }
		public string CustomerId { get; set; }
		public string AuthorizationType { get; set; }
		public string PreviouseAuthorizationType { get; set; }
		public int? Status { get; set; }
		public string ReasonForDeclining { get; set; }
		public string ReasonForDeactivation { get; set; }
	}




}