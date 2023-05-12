using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Modules.Transaction.Dto.Interbank;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Transaction.Validation
{
    public class InitiaBulkTransactionValidation : AbstractValidator<InitiateBulkTransaction>
    {
        public InitiaBulkTransactionValidation()
        {
        RuleFor(p => p.SourceAccountNumber.Trim())
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.Narration)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();
        RuleFor(p => p.Otp)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MinimumLength(4)
            .MaximumLength(6).WithMessage("{PropertyName} only 4 or 6 digit is allow");
        }
    }
    public class InitiaIntraBankTransactionValidation : AbstractValidator<IntraBankTransaction>
    {
        public InitiaIntraBankTransactionValidation()
        {
        RuleFor(p => p.Amount)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();
        // RuleFor(p => p.DestinationAccountNumber)
        //     .NotEmpty().WithMessage("{PropertyName} is required.")
        //     .NotNull()
        //     .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.SourceAccountNumber.Trim())
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.Narration)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();
        RuleFor(p => p.Otp.Trim())
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
            .MinimumLength(4)
            .MaximumLength(8).WithMessage("{PropertyName} only 4 or 8 digit is allow");
        }
    }


    public class InitiaInterBankTransactionValidation : AbstractValidator<InterBankTransaction>
    {
        public InitiaInterBankTransactionValidation()
        {
        RuleFor(p => p.Amount)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();
        // RuleFor(p => p.DestinationAccountNumber)
        //     .NotEmpty().WithMessage("{PropertyName} is required.")
        //     .NotNull()
        //     .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.SourceAccountNumber.Trim())
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.Narration)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();
        RuleFor(p => p.Otp.Trim())
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
            .MinimumLength(4)
            .MaximumLength(8).WithMessage("{PropertyName} only 4 or 8 digit is allow");
        }
    }

}