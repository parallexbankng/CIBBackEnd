using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Services.Api.Dto
{
    public class RequestDto
    {
    }

    public class BankDto
    {
        public  string InstitutionCode { get; set; }
        public  string InstitutionName { get; set; }
        public  string CbnCode { get; set; }
    }

    public class InterbankNameEnquiryModel
    {
        public string BankId { get; set; }
        public string BankCode { get; set; }
        public string accountNumber { get; set; }
    }

    public class InterbankBalanceEnquiryModel
    {
        public string accountNumber { get; set; }
    }

    public class InterbankFundsTransferModel
    {
        public string destinationInstitutionCode { get; set; }
        public string channelCode { get; set; }
        public string narration { get; set; }
        public string paymentReference { get; set; }
        public string amount { get; set; }
        public string beneficiaryAccountNumber { get; set; }
        public string beneficiaryAccountName { get; set; }
        public string originatorAccountName { get; set; }
        public string nameEnquiryRef { get; set; }
        public string beneficiaryBankVerificationNumber { get; set; }
        public string beneficiaryKYCLevel { get; set; }
        public string originatorAccountNumber { get; set; }
        public string originatorBankVerificationNumber { get; set; }
        public string originatorKYCLevel { get; set; }
        public string transactionLocation { get; set; }
    }

    public class InterbankNameEnquiryFormModel
    {
        public string destinationBankCode { get; set; }
        public string accountNumber { get; set; }
    }

    public class InterbankRequestModel
    {
        public string dataValue { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class BankAccountModel
    {
        public string Id { get; set; }
        public string BankName { get; set; }
        public string BranchCode { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal BookBalance { get; set; }
        public decimal BlockedAmount { get; set; }
        public string AccountType { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencyCode { get; set; }
        public string AccountBranch { get; set; }
        public string CustomerNumber { get; set; }
        public string BVN { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ADUserRequestDto : BaseUpdateDto
    {
        public string UserName { get; set; }
    }

    public class ADLoginRequestDto : BaseUpdateDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Otp { get; set; }
    }
    public class ADLoginDto : BaseUpdateDto
    {
        public string Data { get; set; }
    }
}