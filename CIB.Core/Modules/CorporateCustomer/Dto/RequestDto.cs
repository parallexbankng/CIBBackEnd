using System;
using CIB.Core.Common;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Modules.CorporateCustomer.Dto
{
    public class CreateCorporateCustomerRequestDto:BaseDto
    {
        public string CompanyName { get; set; }
        public string Email1 { get; set; }
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string AuthorizationType { get; set; }
        public string PhoneNumber { get; set; }
  }
    public class StatementOfAccount
    {
        public string Channel { get; set; }
        public string AccountNumber { get; set; }
        public string Period { get; set; }
        public string DocumentType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string SendToEmail { get; set; }
        public string SendTo3rdPardy { get; set; }
        public string RecipientEmail { get; set; }
        public string TypeOfDestination { get; set; }
        public string DestinationCode { get; set; }
    }

     public class StatementOfAccountRequestDto
    {
        public string Channel { get; set; }
        public string AccountNumber { get; set; }
        public string Period { get; set; }
        public string? DocumentType { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public bool SendToEmail { get; set; }
        public bool SendTo3rdPardy { get; set; }
        public string RecipientEmail { get; set; }
        public string TypeOfDestination { get; set; }
        public string DestinationCode { get; set; }
    }
    public class UpdateCorporateCustomerRequestDto : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string Email1 { get; set; }
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string AuthorizationType { get; set; }
    }

    public class UpdateCorporateCustomer:BaseUpdateDto
    {
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public string Email1 { get; set; }
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string AuthorizationType { get; set; }
    }

     public class ValidateCorporateCustomerRequestDto :BaseDto
    {
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string AuthorizationType { get; set; }
    }

    public class UpdateAccountLimitRequestDto : BaseUpdateDto
    {
        public string Id {get;set;}
        public bool IsApprovalByLimit { get; set; }
        public string CorporateCustomerId { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
    }

    public class UpdateAccountLimitRequest:BaseUpdateDto
    {
        public string IsApprovalByLimit { get; set; }
        public string CorporateCustomerId { get; set; }
        public string MinAccountLimit { get; set; }
        public string MaxAccountLimit { get; set; }
        public string? SingleTransDailyLimit { get; set; }
        public string? BulkTransDailyLimit { get; set; }
    }

    public class AccountLimitRequestDto:BaseDto
    {
        public string IsApprovalByLimit { get; set; }
        public string CorporateCustomerId { get; set; }
        public string MinAccountLimit { get; set; }
        public string MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public string? SingleTransDailyLimit { get; set; }
        public string? BulkTransDailyLimit { get; set; }
    }

    public class AccountLimitRequest:BaseDto
    {
        public bool IsApprovalByLimit { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
        public string AuthorizationType { get; set; }
    }

    public class OnboardCorporateCustomerRequestDto : BaseDto
  {
    public string CompanyName { get; set; }
    public string Email1 { get; set; }
    public string CorporateEmail {get;set;}
    public string CustomerId { get; set; }
    public string DefaultAccountNumber { get; set; }
    public string DefaultAccountName { get; set; }
    public string CorporateCustomerId { get; set; }
    public string CorporateRoleId { get; set; }
    public string Username { get; set; }
    //public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string ApprovalLimit { get; set; }
    public string MinAccountLimit { get; set; }
    public string MaxAccountLimit { get; set; }
     public string AuthorizationType { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string IsApprovalByLimit { get; set; }
    public string Password { get; set; }
    public string SingleTransDailyLimit {get;set;}
    public string BulkTransDailyLimit {get;set;}
  }

    public class BulkOnboardCorporateCustomerRequestDto : BaseDto
    {
      public IFormFile files { get; set; }
    }
    public class OnboardCorporateCustomer : BaseDto
    {
    public string CompanyName { get; set; }
    public string Email1 { get; set; }
    public string CorporateEmail {get;set;}
    public string CustomerId { get; set; }
    public string DefaultAccountNumber { get; set; }
    public string DefaultAccountName { get; set; }
    public Guid? CorporateCustomerId { get; set; }
    public Guid? CorporateRoleId { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public decimal? MinAccountLimit { get; set; }
    public decimal? MaxAccountLimit { get; set; }
    public string AuthorizationType { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsApprovalByLimit { get; set; }
    public string Password { get; set; }
    public decimal? SingleTransDailyLimit {get;set;}
    public decimal? BulkTransDailyLimit {get;set;}
  }

    public class AddBeneficiaryDto: BaseDto
    {
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string AccountName { get; set; }
        public string BankName { get; set; }
    }

    public class RemoveBeneficiaryDto : BaseUpdateDto
  {
      public string beneficiaryId { get; set; }
    }
    public class RemoveBeneficiary : BaseUpdateDto
  {
      public Guid beneficiaryId { get; set; }
    }

    public class CreateAccountLimitFormModel : BaseDto
    {
        public string IsApprovalByLimit { get; set; }
        public string CorporateCustomerId { get; set; }
        public string? MinAccountLimit { get; set; }
        public string? MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public string? SingleTransDailyLimit { get; set; }
        public string? BulkTransDailyLimit { get; set; }
    }
    public class CreateAccountLimitDto: BaseDto
    {
        public bool IsApprovalByLimit { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
    }

     public class CreateAccountLimitModel : BaseDto
    {
        public bool IsApprovalByLimit { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
    }

    public class SetAccountLimitModel
    {
        public bool IsApprovalByLimit { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public decimal? SingleTransDailyLimit { get; set; }
        public decimal? BulkTransDailyLimit { get; set; }
    }

    public class BulkCustomerOnboading 
    {
        public string CompanyName { get; set; }
        public string Email1 { get; set; }
        public string CorporateEmail {get;set;}
        public string CustomerId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? CorporateRoleId { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string MiddleName { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public decimal? MinAccountLimit { get; set; }
        public decimal? MaxAccountLimit { get; set; }
        public string AuthorizationType { get; set; }
        public bool IsApprovalByLimit { get; set; }
        public string Password { get; set; }
        public decimal? SingleTransDailyLimit {get;set;}
        public decimal? BulkTransDailyLimit {get;set;}
        public string? Error {get;set;}

    }

    public class BulkSendCreditial
    {
        public string Password {get; set;}
        public Guid CorporateCustomerId {get;set;}
    }



}