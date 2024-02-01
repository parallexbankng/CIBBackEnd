
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.CorporateCustomer.Mapper
{
    public class CorporateCustomerValidation : AbstractValidator<CreateCorporateCustomerRequestDto>
    {
        public CorporateCustomerValidation()
        {
            RuleFor(p => p.CompanyName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CorporateShortName.Trim())
              .NotEmpty().WithMessage("{PropertyName} is required.")
              .NotNull();
            RuleFor(p => p.CustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.DefaultAccountName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not Account Number.")
                .NotNull();
            RuleFor(p => p.DefaultAccountNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not a valid Account Number.")
                .NotNull();
            RuleFor(p => p.Email1)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().EmailValidation).WithMessage("{PropertyName} Email is not valid.")
                .NotNull();
            RuleFor(p => p.AuthorizationType)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
    public class UpdateCorporateCustomerValidation : AbstractValidator<UpdateCorporateCustomerRequestDto>
    {
        public UpdateCorporateCustomerValidation()
        {
            RuleFor(p => p.CompanyName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CorporateShortName)
               .NotEmpty().WithMessage("{PropertyName} is required.")
               .NotNull();
            RuleFor(p => p.CustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.DefaultAccountName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.DefaultAccountNumber)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Email1)
                .Matches(new ReqEx().EmailValidation).WithMessage("{PropertyName} Email is not valid.")
                .NotNull();
            RuleFor(p => p.AuthorizationType)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }

    public class UpdateCorporateCustomerShortNameValidation : AbstractValidator<UpdateCorporateCustomerShortNameRequestDto>
    {
        public UpdateCorporateCustomerShortNameValidation()
        {
            RuleFor(p => p.CorporateShortName)
               .NotEmpty().WithMessage("{PropertyName} is required.")
               .NotNull();
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }

    public class CreateLimitCorporateCustomerValidation : AbstractValidator<UpdateAccountLimitRequestDto>
    {
        public CreateLimitCorporateCustomerValidation()
        {
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.MaxAccountLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.MinAccountLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.SingleTransDailyLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.BulkTransDailyLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
    public class ValidateCorporateCustomerValidation : AbstractValidator<ValidateCorporateCustomerRequestDto>
    {
        public ValidateCorporateCustomerValidation()
        {
            RuleFor(p => p.CompanyName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.DefaultAccountName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid Account Number.")
                .NotNull();
            RuleFor(p => p.DefaultAccountNumber)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not a valid Account Number.")
                .NotNull();
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().EmailValidation).WithMessage("{PropertyName} Email is not valid.")
                .NotNull();
            RuleFor(p => p.AuthorizationType)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
    public class OnboardCorporateCustomerValidation : AbstractValidator<OnboardCorporateCustomer>
    {
        public OnboardCorporateCustomerValidation()
        {
            RuleFor(p => p.CompanyName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.DefaultAccountName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid Account Number.")
                .NotNull();
            RuleFor(p => p.DefaultAccountNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not a valid Account Number.")
                .NotNull();
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().EmailValidation).WithMessage("{PropertyName} Email is not valid.")
                .NotNull();
            RuleFor(p => p.AuthorizationType)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.PhoneNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.FirstName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.MinAccountLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.MaxAccountLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.SingleTransDailyLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.BulkTransDailyLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
}