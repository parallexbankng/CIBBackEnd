
using CIB.Core.Modules.CorporateProfile.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.CorporateProfile.Validation
{
    public class CreateCorporateProfileValidation : AbstractValidator<CreateProfileDto>
    {
        public CreateCorporateProfileValidation()
        {
            RuleFor(p => p.CorporateRoleId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Phone.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .EmailAddress().WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.FirstName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.ApprovalLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }

    public class UpdateCorporateProfileValidation : AbstractValidator<UpdateProfileDTO>
    {
        public UpdateCorporateProfileValidation()
        {

            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            // RuleFor(p => p.CorporateRoleId)
            //     .NotEmpty().WithMessage("{PropertyName} is required.")
            //     .NotNull();
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Phone.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.FirstName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            // RuleFor(p => p.ApprovalLimit)
            //     .NotEmpty().WithMessage("{PropertyName} is required.")
            //     .NotNull();
        }
    }

    public class DeclineCorporateProfileValidation : AbstractValidator<DeclineProfileDTO>
    {
        public DeclineCorporateProfileValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.Reason).NotEmpty().WithMessage("{PropertyName} is required.").NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }
    public class DeactivateCorporateProfileValidation : AbstractValidator<DeactivateProfileDTO>
    {
        public DeactivateCorporateProfileValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.Reason).NotEmpty().WithMessage("{PropertyName} is required.").NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }
    public class UpdateCorporateProfileUserRoleValidation : AbstractValidator<UpdateProfileUserRoleDTO>
    {
        public UpdateCorporateProfileUserRoleValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.RoleId).NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }

    public class UpdateCorporateProfileUserNameValidation : AbstractValidator<UpdateProfileUserNameDTO>
    {
        public UpdateCorporateProfileUserNameValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.UserName).NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }

}