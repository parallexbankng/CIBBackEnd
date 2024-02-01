using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.BulkTransaction.Validation
{
    public class VerifyBulkTransactionValidator : AbstractValidator<VerifyBulkTransaction>
    {
        public VerifyBulkTransactionValidator()
        {
            RuleFor(p => p.SourceAccountNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().NumberOnly.ToString().Trim()).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Narration.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            // RuleFor(p => p.Amount)
            //     .NotEmpty().WithMessage("{PropertyName} is required.")
            //     .NotNull();
        }
    }
}