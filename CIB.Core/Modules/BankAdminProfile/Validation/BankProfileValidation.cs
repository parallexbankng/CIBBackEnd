using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CIB.Core.Modules.BankAdminProfile.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.BankAdminProfile.Validation
{

    public class CreateBankAdminProfileValidation: AbstractValidator<CreateBankAdminProfileDTO>
    {
        public CreateBankAdminProfileValidation()
        {
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.PhoneNumber.Trim())
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .MinimumLength(11).WithMessage("{PropertyName} minimum of 11 digit.")
                .MaximumLength(15).WithMessage("{PropertyName} must not exceed 11 characters.");
            RuleFor(p => p.Email.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .EmailAddress().WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.FirstName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
        }
    }
    public class UpdateBankAdminProfileValidation: AbstractValidator<UpdateBankAdminProfileDTO>
    {
        public UpdateBankAdminProfileValidation()
        {
            RuleFor(p => p.Username)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.PhoneNumber)
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .MinimumLength(11).WithMessage("{PropertyName} minimum of 11 digit.")
                .MaximumLength(15).WithMessage("{PropertyName} must not exceed 11 characters.");
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 10 characters.");
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 10 characters.");
            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 10 characters.");
            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 10 characters.");
        }
    }

    public class DeclineBankAdminProfileValidation: AbstractValidator<DeclineBankAdminProfileDTO>
    {
        public DeclineBankAdminProfileValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.Reason)
                .NotEmpty().WithMessage("{PropertyName} is required.").NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }
    public class DeactivateBankAdminProfileValidation: AbstractValidator<DeactivateBankAdminProfileDTO>
    {
        public DeactivateBankAdminProfileValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.Reason)
                .NotEmpty().WithMessage("{PropertyName} is required.").NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    }
    public class UpdateBankAdminProfileUserRoleValidation: AbstractValidator<UpdateBankAdminProfileUserRoleDTO>
    {
        public UpdateBankAdminProfileUserRoleValidation()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.RoleId).NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }
}