
using CIB.Core.Modules.UserAccess.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.UserAccess.Validation
{
    public class CreateUserAccessValidation:AbstractValidator<CreateRequestDto>
    {
        public CreateUserAccessValidation()
        {
                RuleFor(p => p.Name)
                    .NotEmpty().WithMessage("{PropertyName} is required.")
                    .NotNull()
                    .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
                RuleFor(p => p.IsCorporate)
                    .NotNull().WithMessage("{PropertyName} is required.");
        }
    }

    public class UpdateUserAccessValidation:AbstractValidator<UpdateRequestDto>
    {
        public UpdateUserAccessValidation()
        {
                RuleFor(p => p.Id)
                    .NotEmpty().WithMessage("{PropertyName} is required.")
                    .NotNull();
                RuleFor(p => p.Name)
                    .NotEmpty().WithMessage("{PropertyName} is required.")
                    .NotNull()
                    .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
                RuleFor(p => p.IsCorporate)
                    .NotNull().WithMessage("{PropertyName} is required.");
        }
    }
}