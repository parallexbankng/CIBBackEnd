using System;

namespace CIB.Core.Common
{
	public class BaseDto
	{
		public BaseDto()
		{
			Id = Guid.NewGuid();
		}
		public Guid Id { get; set; }
		public string? ClientStaffIPAddress { get; set; }
		public string? IPAddress { get; set; }
		public string? HostName { get; set; }
		public string? MACAddress { get; set; }
	}
	public class BaseUpdateDto
	{
		public string? ClientStaffIPAddress { get; set; }
		public string? IPAddress { get; set; }
		public string? HostName { get; set; }
		public string? MACAddress { get; set; }
	}
	public class AuthorizationTypeModel
	{
		public string Key { get; set; }
		public string Name { get; set; }
	}
	public class TransactionTypeModel
	{
		public string Key { get; set; }
		public string Name { get; set; }
	}
	public class SimpleActionDto : BaseUpdateDto
	{
		public string Id { get; set; }
		public string Reason { get; set; }
	}
	public class SimpleAction : BaseUpdateDto
	{
		public Guid Id { get; set; }
		public Guid? CorporateCustomerId { get; set; }
		public string Reason { get; set; }
	}

	public class BulkError
	{
		public string Message { get; set; }
		public string ActionInfo { get; set; }
		public Guid? Id { get; set; }
	}
	// public class UpdateAction: BaseUpdateDto
	// {
	//     public Guid Id { get; set; }
	//     public string Beneficiaries {get;set;}
	// }
	public class UpdateActionDto : BaseUpdateDto
	{
		public string Id { get; set; }
		public string Data { get; set; }
	}
	public class AppAction : BaseUpdateDto
	{
		public string Id { get; set; }
		public string Reason { get; set; }
	}
	public class AppActionDto : BaseUpdateDto
	{
		public Guid Id { get; set; }
		public string Reason { get; set; }
	}

	public class EmailNotification
	{
		public string CustomerId { get; set; }
		public string? CompanyName { get; set; }
		public string? UserName { get; set; }
		public string? PreviouseUserName { get; set; }
		public string? CorporateShortName { get; set; }
		public string? AggregatedAccounts { get; set; }
		public string Role { get; set; }
		public string PreviousRole { get; set; }
		public string Email { get; set; }
		public string AccountNumber { get; set; }
		public string AccountName { get; set; }
		public string FullName { get; set; }
		public string Reason { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MiddleName { get; set; }
		public string PhoneNumber { get; set; }
		public string Action { get; set; }
		public string RequestType { get; set; }
		public string Amount { get; set; }
		public string AuthorizationType { get; set; }
		public decimal? MinAccountLimit { get; set; }
		public decimal? MaxAccountLimit { get; set; }
		public decimal? BulkTransDailyLimit { get; set; }
		public decimal? SingleTransDailyLimit { get; set; }
		public string WorkflowName { get; set; }
		public string Description { get; set; }
		public int? NoOfAuthorizers { get; set; }
		public decimal? ApprovalLimit { get; set; }
	}

	public class AuditTrailDetail
	{
		public Guid UserId { get; set; }
		public string Ipaddress { get; set; }
		public string Macaddress { get; set; }
		public string HostName { get; set; }
		public string ClientStaffIpaddress { get; set; }
		public string UserName { get; set; }
		public string Description { get; set; }
		public string Action { get; set; }
		public string NewFieldValue { get; set; }
		public string PreviousFieldValue { get; set; }
		public string? TransactionId { get; set; }
	}

	public class ActionStatus
	{
		public string Message { get; set; }
		public bool IsExist { get; set; }
	}

	public class GenericRequestDto : BaseDto
	{
		public string Data { get; set; }
	}
	public class GenericStatus
	{
		public string Message { get; set; }
		public bool Status { get; set; }
	}

}
