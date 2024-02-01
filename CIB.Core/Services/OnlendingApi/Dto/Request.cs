
using System;
using CIB.Core.Modules.Transaction.Dto;

namespace CIB.Core.Services.OnlendingApi.Dto
{
    public class OnlendingBeneficiaryAccountOpeningRequest
    {
        public string BVN { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string MaritalStatus { get; set; }
        public string Gender { get; set; }
        public string streetNo { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string LGA { get; set; }
        public string Region { get; set; }
        public string DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; }
        public string CountryOfResidence { get; set; }
        public string EmploymentStatus { get; set; }
        public string Occupation { get; set; }
        public string Nationality { get; set; }
        public string ReferralCode { get; set; }
        public string ChannelCode { get; set; }
        public string SchmCode { get; set; }
        public string AccountType { get; set; }
        public string StateOfResidence { get; set; }
        public string RequestID { get; set; }
    }

    public class OnlendingIntrestCalculateRequest
    {
        public string Amount { get; set; }
        public string AccountNumber { get; set; }
        public string Durationr { get; set; }
        public string endDate { get; set; }
        public string DrawingPower { get; set; }
        public string SanctionLimit { get; set; }
    }

    public class OnlendingValidateManagementFeeRequest
    {
        public string AccountNumber { get; set; }
        public string DrawingPowerAmount { get; set; }
    }

    public class OnlendingInitiateMerchantDisburstment
    {
        public decimal? ApprovedAmount { get; set; }
        public decimal? RequestedAmount { get; set; }
        public string MerchantCustomerId { get; set; }
        public string MerchantOperatingAccountNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class OnlendingInitiateBeneficiaryDisburstment
    {
        public string RequestId { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string beneficiaryAccountNumber { get; set; }
        public string merchantAccountNumber { get; set; }
        public string MerchantOperatingAccountNumber { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    public class OnlendingGetInterestRequest
    {
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public int? DurationIndays { get; set; }
    }

    public class OnlendingInitiateExtensionRequest
    {
        public string RequestId { get; set; }
        public int BeneficiaryId { get; set; }
        public string BeneficiaryAccountNumber { get; set; }
        public string MerchantOperatingAccountNumber { get; set; }
        public int? DurationIndays { get; set; }
    }

    public class OnlendingPreLiquidationRequest
    {
        public string RequestId { get; set; }
        public int BeneficiaryId { get; set; }
        public string MerchantOperatingAccountNumber { get; set; }
        public string BeneficiaryAccountNumber { get; set; }
        public decimal Amount { get; set; }
    }

    public class OnlendingFullLiquidationRequest
    {
        public string RequestId { get; set; }
        public int BeneficiaryId { get; set; }
        public string MerchantOperatingAccountNumber { get; set; }
        public string BeneficiaryAccountNumber { get; set; }
    }

    public class OnLendingBvnValidationRequest
    {

    }

    public class OnLendingIdIssueValidationRequest
    {

    }

    public class OnLendingInitiateBatchRequest : BaseTransactioDto
    {
        public string BatchId { get; set; }
    }

}

