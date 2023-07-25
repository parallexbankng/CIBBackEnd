using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Authentication.Validation
{
    public class CustomerValidation : AbstractValidator<CustomerLoginParam>
    {
        public CustomerValidation()
        {
            RuleFor(p => p.Username.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Password.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MinimumLength(5).WithMessage("{PropertyName} must be more than 5 characters.");
            RuleFor(p => p.CustomerID.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        }
    
    }
}