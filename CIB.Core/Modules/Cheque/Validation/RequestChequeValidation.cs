using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.Cheque.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Cheque.Validation
{
    public class RequestChequeValidation : AbstractValidator<RequestChequeBookDto>
    {
       
        public RequestChequeValidation()
        {
            RuleFor(p => p.AccountNumber)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.AccountType)
                .NotEmpty().WithMessage("{PropertyName} is required.");
            RuleFor(p => p.PickupBranch)
                .NotEmpty().WithMessage("{PropertyName} is required");
            RuleFor(p => p.NumberOfLeave)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        }
        
    }
}