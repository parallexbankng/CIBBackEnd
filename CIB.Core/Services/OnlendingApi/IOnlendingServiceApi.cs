
using System.Threading.Tasks;
using CIB.Core.Services.OnlendingApi.Dto;

namespace CIB.Core.Services.OnlendingApi
{
    public interface IOnlendingServiceApi
    {
        Task<OnleandBvnValidationResponse> ValidateBvn(string Bvn);
        Task<OnleandIdIssueValidationResponse> ValidateNIN(string ninNumber);
        Task<OnleandIdIssueValidationResponse> ValidatePassport(string passportId);
        Task<OnlendingAuthTokenResponse> GetAuthToken();
        Task<OnlendingAccountOpeningResponse> AccountOpening(OnlendingBeneficiaryAccountOpeningRequest request);
        Task<OnleandingResponse> CalculateInterest(OnlendingGetInterestRequest request);
        Task<OnlendingDateExtendsionResponse> InitiateRepaymentDateExtension(OnlendingInitiateExtensionRequest request);
        Task<OnlendingLiquidationResponse> PreliquidatePayment(OnlendingPreLiquidationRequest request);
        Task<OnlendingLiquidationResponse> LiquidatePayment(OnlendingFullLiquidationRequest request);
        Task<OnleandingResponse> InitiateMatchantDisbursment(OnlendingInitiateMerchantDisburstment request);
        Task<OnleandingDesbursmentResponse> InitiateBeneficiaryDisbursment(OnlendingInitiateBeneficiaryDisburstment request);
        Task<OnlendingMerchantBeneficiaryResponse> GetMerchantBeneficiaries(string merchantAccountNumber);
		    Task<OnlendingMerchantResponse> GetMerchant(string customerId);
		    Task<OnleandingResponse> ValidateManagmentFee(OnlendingValidateManagementFeeRequest request);
        Task<OnleandBvnValidationResponse> TestValidateBvn(string Bvn);
        Task<BeneficiaryAdditionInfoRespons> TestGetBeneficiaryAddressInfo();
    }
}